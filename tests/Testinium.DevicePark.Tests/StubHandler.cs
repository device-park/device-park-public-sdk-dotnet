using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Testinium.DevicePark.Tests;

public sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    string? Body,
    IReadOnlyDictionary<string, string> Headers);

public sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<RecordedRequest, int, HttpResponseMessage> _responder;

    public StubHandler(Func<RecordedRequest, int, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public List<RecordedRequest> Requests { get; } = new List<RecordedRequest>();

    public List<RecordedRequest> ApiRequests { get; } = new List<RecordedRequest>();

    public int TokenRequestCount { get; private set; }

    public static StubHandler Json(string payload, HttpStatusCode status = HttpStatusCode.OK) =>
        new StubHandler((_, _) => new HttpResponseMessage(status)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        });

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            headers[header.Key] = string.Join(",", header.Value);
        }

        var recorded = new RecordedRequest(request.Method, request.RequestUri!, body, headers);
        Requests.Add(recorded);

        if (recorded.Uri.AbsolutePath.EndsWith("/uaa/oauth2/token", StringComparison.Ordinal))
        {
            TokenRequestCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"access_token\":\"token-" + TokenRequestCount + "\",\"token_type\":\"Bearer\",\"expires_in\":3600}",
                    Encoding.UTF8,
                    "application/json")
            };
        }

        ApiRequests.Add(recorded);
        return _responder(recorded, ApiRequests.Count);
    }
}
