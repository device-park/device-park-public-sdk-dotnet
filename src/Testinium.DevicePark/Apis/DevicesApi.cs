using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Testinium.DevicePark.Errors;
using Testinium.DevicePark.Internal;
using Testinium.DevicePark.Models;

namespace Testinium.DevicePark.Apis;

public sealed class DevicesApi
{
    private const string DevicesPath = "/management/api/v2/public/devices";
    private const string DeviceDetailPath = "/management/api/v1/public/devices/";

    private readonly DeviceParkHttpClient _httpClient;
    private readonly PoolsApi _poolsApi;

    internal DevicesApi(DeviceParkHttpClient httpClient, PoolsApi poolsApi)
    {
        _httpClient = httpClient;
        _poolsApi = poolsApi;
    }

    public async Task<PageDto<Device>> ListAsync(
        ListDevicesRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var effective = request ?? new ListDevicesRequest.Builder().Build();
        var response = await _httpClient
            .GetAsync(DevicesPath, Query.Pagination(effective.Sorting, effective.Filters), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<PageDto<Device>>(response);
    }

    public async Task<Device> GetAsync(string serial, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serial))
        {
            throw new DeviceParkConfigException("serial cannot be empty");
        }

        var response = await _httpClient
            .GetAsync(DeviceDetailPath + Uri.EscapeDataString(serial), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<Device>(response);
    }

    // DP-PARITY:JAVA-ONLY  Node SDK'da cihaz tarafinda default-pool kisayolu yok; Node bunu pools().listByDefaultPool ile cozuyor.
    public async Task<PageDto<Device>> ListByDefaultPoolAsync(
        ListDevicesRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var effective = request ?? new ListDevicesRequest.Builder().Build();
        var defaultPool = await _poolsApi.GetDefaultPoolAsync(cancellationToken).ConfigureAwait(false);

        var builder = new ListDevicesRequest.Builder()
            .Page(effective.Sorting.Page)
            .Size(effective.Sorting.Size)
            .SortBy(effective.Sorting.SortBy)
            .Direction(effective.Sorting.Direction)
            .AddFilter(DeviceFilter.POOL_ID, defaultPool.Id, SearchOperation.EQUAL);

        foreach (var filter in effective.Filters)
        {
            if (filter.Key == DeviceFilter.POOL_ID)
            {
                continue;
            }

            builder.AddFilter(filter.Key, filter.Value, filter.Operation);
        }

        return await ListAsync(builder.Build(), cancellationToken).ConfigureAwait(false);
    }

    // DP-PARITY:FROM-NODE  Java SDK'da cihazdaki kurulu uygulamalari listeleyen bir metot yok.
    public async Task<PageDto<DeviceApp>> AppsAsync(
        string serial,
        ListDeviceAppsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serial))
        {
            throw new DeviceParkConfigException("serial cannot be empty");
        }

        var effective = request ?? new ListDeviceAppsRequest.Builder().Build();
        var path = DeviceDetailPath + Uri.EscapeDataString(serial) + "/apps";

        var response = await _httpClient
            .GetAsync(path, Query.Pagination(effective.Sorting, effective.Filters), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<PageDto<DeviceApp>>(response);
    }
}
