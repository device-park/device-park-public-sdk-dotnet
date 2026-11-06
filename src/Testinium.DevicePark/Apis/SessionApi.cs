using System;
using System.Threading;
using System.Threading.Tasks;
using Testinium.DevicePark.Errors;
using Testinium.DevicePark.Internal;
using Testinium.DevicePark.Models;

namespace Testinium.DevicePark.Apis;

public sealed class SessionApi
{
    private const string SessionPath = "/session/api/v2/public/sessions";
    private const string ScreenRecordPath = "/storage/api/v1/public/sessions";

    private readonly DeviceParkHttpClient _httpClient;

    internal SessionApi(DeviceParkHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PageDto<Session>> ListAsync(
        DeviceSessionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var effective = request ?? new DeviceSessionRequest.Builder().Build();
        var response = await _httpClient
            .GetAsync(SessionPath, Query.Pagination(effective.Sorting, effective.Filters), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<PageDto<Session>>(response);
    }

    public async Task<Session> StartAsync(
        DeviceStartSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new DeviceParkConfigException("request is required");
        }

        var response = await _httpClient
            .PostAsync(SessionPath, JsonMapper.ToJson(request), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<Session>(response);
    }

    // The session-api response carries the stream address without a token; this method appends the
    // SDK's current access token as the "token" query parameter. The returned URL must not be logged.
    public async Task<string> StreamUrlAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        EnsureSessionId(sessionId);

        var path = SessionPath + "/" + Uri.EscapeDataString(sessionId) + "/stream-url";
        var response = await _httpClient
            .GetAsync(path, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var streamUrl = JsonMapper.FromJson<StreamUrl>(response);
        var accessToken = await _httpClient.CurrentAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        return StreamUrls.WithAccessToken(streamUrl.Url!, accessToken);
    }

    // Returns a one-time URL carrying a single-use ticket instead of an access token. Each call
    // creates a new ticket, so a returned URL must not be reused - including after a failed
    // connection attempt - and must not be logged. StreamUrlAsync stays available for existing
    // integrations; new ones should use this method.
    public async Task<string> StreamTicketUrlAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        EnsureSessionId(sessionId);

        var path = SessionPath + "/" + Uri.EscapeDataString(sessionId) + "/stream-ticket";
        var response = await _httpClient
            .PostAsync(path, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var streamUrl = JsonMapper.FromJson<StreamUrl>(response);

        if (string.IsNullOrWhiteSpace(streamUrl.Url))
        {
            throw new DeviceParkException("stream ticket URL is empty");
        }

        return streamUrl.Url!;
    }

    public async Task StopAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        EnsureSessionId(sessionId);

        await _httpClient
            .DeleteAsync(SessionPath + "/" + Uri.EscapeDataString(sessionId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<byte[]> LogsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        EnsureSessionId(sessionId);

        return await _httpClient
            .GetBytesAsync(SessionPath + "/" + Uri.EscapeDataString(sessionId) + "/appium-log", cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PageDto<ScreenRecord>> ScreenRecordsAsync(
        string sessionId,
        ScreenRecordPaginationRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        EnsureSessionId(sessionId);

        var effective = request ?? new ScreenRecordPaginationRequest.Builder().Build();
        var path = ScreenRecordPath + "/" + Uri.EscapeDataString(sessionId) + "/screen-records";

        var response = await _httpClient
            .GetAsync(path, Query.Pagination(effective.Sorting, effective.Filters), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<PageDto<ScreenRecord>>(response);
    }

    private static void EnsureSessionId(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new DeviceParkConfigException("sessionId cannot be empty");
        }
    }
}
