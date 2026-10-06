using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Testinium.DevicePark.Errors;
using Testinium.DevicePark.Internal;
using Testinium.DevicePark.Models;

namespace Testinium.DevicePark.Apis;

public sealed class PoolsApi
{
    private const string PoolsPath = "/management/api/v1/public/pools";

    private readonly DeviceParkHttpClient _httpClient;

    internal PoolsApi(DeviceParkHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PageDto<Pool>> ListAsync(
        ListPoolsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var effective = request ?? new ListPoolsRequest.Builder().Build();
        var response = await _httpClient
            .GetAsync(PoolsPath, Query.Pagination(effective.Sorting, effective.Filters), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<PageDto<Pool>>(response);
    }

    // DP-PARITY:JAVA-ONLY  Node SDK tek bir Pool dondurmez; yalnizca sayfa donduren listByDefaultPool'u vardir.
    public async Task<Pool> GetDefaultPoolAsync(CancellationToken cancellationToken = default)
    {
        var page = await ListByDefaultPoolAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        if (page.IsEmpty)
        {
            throw new DeviceParkException("Default pool not found");
        }

        return page.Data[0];
    }

    // DP-PARITY:FROM-NODE  Java SDK'da sayfa donduren default-pool metodu yok; yalnizca tek nesne donduren getDefaultPool var.
    public async Task<PageDto<Pool>> ListByDefaultPoolAsync(
        ListPoolsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var effective = request ?? new ListPoolsRequest.Builder().Build();

        var builder = new ListPoolsRequest.Builder()
            .Page(effective.Sorting.Page)
            .Size(effective.Sorting.Size)
            .SortBy(effective.Sorting.SortBy)
            .Direction(effective.Sorting.Direction)
            .AddFilter(PoolFilter.IS_DEFAULT, true, SearchOperation.EQUAL);

        foreach (var filter in effective.Filters)
        {
            if (filter.Key == PoolFilter.IS_DEFAULT)
            {
                continue;
            }

            builder.AddFilter(filter.Key, filter.Value, filter.Operation);
        }

        return await ListAsync(builder.Build(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<Pool> CreateAsync(CreatePoolRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new DeviceParkConfigException("request is required");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new DeviceParkConfigException("name cannot be empty");
        }

        var response = await _httpClient
            .PostAsync(PoolsPath, JsonMapper.ToJson(request), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<Pool>(response);
    }

    public async Task DeleteAsync(string poolId, CancellationToken cancellationToken = default)
    {
        EnsurePoolId(poolId);

        await _httpClient
            .DeleteAsync(PoolsPath + "/" + Uri.EscapeDataString(poolId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> AddDevicesAsync(
        string poolId,
        IReadOnlyList<string> serials,
        CancellationToken cancellationToken = default)
    {
        EnsurePoolIdAndSerials(poolId, serials);

        var response = await _httpClient
            .PostAsync(
                PoolsPath + "/" + Uri.EscapeDataString(poolId) + "/devices",
                jsonBody: null,
                query: SerialsQuery(serials),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<List<string>>(response);
    }

    public async Task<IReadOnlyList<string>> RemoveDevicesAsync(
        string poolId,
        IReadOnlyList<string> serials,
        CancellationToken cancellationToken = default)
    {
        EnsurePoolIdAndSerials(poolId, serials);

        var response = await _httpClient
            .DeleteAsync(
                PoolsPath + "/" + Uri.EscapeDataString(poolId) + "/devices",
                query: SerialsQuery(serials),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<List<string>>(response);
    }

    private static List<KeyValuePair<string, object?>> SerialsQuery(IReadOnlyList<string> serials) =>
        new List<KeyValuePair<string, object?>>
        {
            new KeyValuePair<string, object?>("serials", serials)
        };

    private static void EnsurePoolId(string poolId)
    {
        if (string.IsNullOrWhiteSpace(poolId))
        {
            throw new DeviceParkConfigException("poolId cannot be empty");
        }
    }

    private static void EnsurePoolIdAndSerials(string poolId, IReadOnlyList<string> serials)
    {
        EnsurePoolId(poolId);

        if (serials is null || serials.Count == 0)
        {
            throw new DeviceParkConfigException("serials cannot be empty");
        }
    }
}
