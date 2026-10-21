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
