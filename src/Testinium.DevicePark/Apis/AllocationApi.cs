using System;
using System.Threading;
using System.Threading.Tasks;
using Testinium.DevicePark.Errors;
using Testinium.DevicePark.Internal;
using Testinium.DevicePark.Models;

namespace Testinium.DevicePark.Apis;

public sealed class AllocationApi
{
    private const string AllocationPath = "/allocation/api/v2/public/allocations";

    private readonly DeviceParkHttpClient _httpClient;

    internal AllocationApi(DeviceParkHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PageDto<Allocation>> ListAsync(
        AllocationSearchRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var effective = request ?? new AllocationSearchRequest.Builder().Build();
        var response = await _httpClient
            .GetAsync(AllocationPath, Query.Pagination(effective.Sorting, effective.Filters), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<PageDto<Allocation>>(response);
    }

    public async Task<Allocation> CreateAsync(
        DeviceAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new DeviceParkConfigException("request is required");
        }

        var response = await _httpClient
            .PostAsync(AllocationPath, JsonMapper.ToJson(request), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<Allocation>(response);
    }

    // DP-PARITY:FROM-NODE  Java delete() bos allocationId'yi dogrulamadan path'e ekliyor.
    public async Task DeleteAsync(string allocationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(allocationId))
        {
            throw new DeviceParkConfigException("allocationId cannot be empty");
        }

        await _httpClient
            .DeleteAsync(AllocationPath + "/" + Uri.EscapeDataString(allocationId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
