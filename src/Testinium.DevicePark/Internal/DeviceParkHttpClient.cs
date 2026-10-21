using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Testinium.DevicePark.Errors;

namespace Testinium.DevicePark.Internal;

// DP-PARITY:JAVA-DEAD  Java DeviceParkHttpClient'ta put(), postMultipart() ve postBytes() metotlari var;
// DP-PARITY:JAVA-DEAD  hicbir API servisi bunlari cagirmiyor. Tasinmadilar.
internal sealed class DeviceParkHttpClient : IDisposable
{
    private const string TokenPath = "/uaa/oauth2/token";

    private static readonly TimeSpan TokenSafetyMargin = TimeSpan.FromSeconds(60);

    private readonly string _baseUrl;
    private readonly Credentials _credentials;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly IReadOnlyDictionary<string, string> _defaultHeaders;
    private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);

    private AccessToken? _cachedToken;
    private bool _disposed;

    internal DeviceParkHttpClient(
        string baseUrl,
        int timeoutSeconds,
        Credentials credentials,
        HttpMessageHandler? handler,
        IReadOnlyDictionary<string, string> defaultHeaders)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new DeviceParkConfigException("baseUrl cannot be empty");
        }

        _baseUrl = baseUrl;
        _credentials = credentials;
        _defaultHeaders = defaultHeaders;
        _ownsHttpClient = handler is null;
        _httpClient = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    internal Task<string> GetAsync(
        string path,
        IEnumerable<KeyValuePair<string, object?>>? query = null,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) =>
        SendForStringAsync(
            () => CreateRequest(HttpMethod.Get, path, query, headers, content: null),
            allowRetry: true,
            cancellationToken);

    internal Task<string> PostAsync(
        string path,
        string? jsonBody = null,
        IEnumerable<KeyValuePair<string, object?>>? query = null,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) =>
        SendForStringAsync(
            () => CreateRequest(
                HttpMethod.Post,
                path,
                query,
                headers,
                jsonBody is null ? null : new StringContent(jsonBody, Encoding.UTF8, "application/json")),
            allowRetry: true,
            cancellationToken);

    internal Task<string> DeleteAsync(
        string path,
        IEnumerable<KeyValuePair<string, object?>>? query = null,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) =>
        SendForStringAsync(
            () => CreateRequest(HttpMethod.Delete, path, query, headers, content: null),
            allowRetry: true,
            cancellationToken);

    internal Task<string> PostStreamAsync(
        string path,
        Stream body,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) =>
        SendForStringAsync(
            () =>
            {
                var content = new StreamContent(body);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                return CreateRequest(HttpMethod.Post, path, query: null, headers, content);
            },
            allowRetry: false,
            cancellationToken);

    internal async Task<byte[]> GetBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
                () => CreateRequest(HttpMethod.Get, path, query: null, headers: null, content: null),
                allowRetry: true,
                cancellationToken)
            .ConfigureAwait(false);

        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _tokenLock.Dispose();

        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string path,
        IEnumerable<KeyValuePair<string, object?>>? query,
        IReadOnlyDictionary<string, string>? headers,
        HttpContent? content)
    {
        var request = new HttpRequestMessage(method, Query.BuildUri(_baseUrl, path, query));

        if (content is not null)
        {
            request.Content = content;
        }

        foreach (var header in _defaultHeaders)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (headers is null)
        {
            return request;
        }

        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return request;
    }

    private async Task<string> SendForStringAsync(
        Func<HttpRequestMessage> requestFactory,
        bool allowRetry,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(requestFactory, allowRetry, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        bool allowRetry,
        CancellationToken cancellationToken)
    {
        var response = await SendOnceAsync(requestFactory(), cancellationToken).ConfigureAwait(false);

        // DP-PARITY:JAVA-ONLY  Node SDK'da 401 sonrasi token yenileme ve tek seferlik tekrar deneme yok.
        if (response.StatusCode == HttpStatusCode.Unauthorized && allowRetry)
        {
            response.Dispose();
            InvalidateCachedToken();
            response = await SendOnceAsync(requestFactory(), cancellationToken).ConfigureAwait(false);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var status = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        response.Dispose();

        throw ErrorEnvelope.ToException(status, body);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await GetValidAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.TryAddWithoutValidation("Authorization", token.AuthorizationHeader);

        return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private void InvalidateCachedToken() => _cachedToken = null;

    private async Task<AccessToken> GetValidAccessTokenAsync(CancellationToken cancellationToken)
    {
        var cached = _cachedToken;
        if (cached is not null && cached.IsValid(TokenSafetyMargin, DateTimeOffset.UtcNow))
        {
            return cached;
        }

        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cached = _cachedToken;
            if (cached is not null && cached.IsValid(TokenSafetyMargin, DateTimeOffset.UtcNow))
            {
                return cached;
            }

            var fetched = await FetchAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            _cachedToken = fetched;
            return fetched;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<AccessToken> FetchAccessTokenAsync(CancellationToken cancellationToken)
    {
        var basicAuth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(_credentials.ClientId + ":" + _credentials.ClientSecret));

        using var request = new HttpRequestMessage(HttpMethod.Post, Query.BuildUri(_baseUrl, TokenPath))
        {
            Content = new StringContent(
                "grant_type=client_credentials&scope=openid",
                Encoding.UTF8,
                "application/x-www-form-urlencoded")
        };

        request.Headers.TryAddWithoutValidation("Authorization", "Basic " + basicAuth);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw ErrorEnvelope.ToException((int)response.StatusCode, body);
        }

        return AccessToken.FromResponse(JsonMapper.FromJson<AccessTokenResponse>(body), DateTimeOffset.UtcNow);
    }
}
