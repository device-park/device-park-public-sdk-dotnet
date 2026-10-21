using System;
using Testinium.DevicePark.Apis;
using Testinium.DevicePark.Internal;

namespace Testinium.DevicePark;

public sealed class DeviceParkApiClient : IDisposable
{
    private readonly DeviceParkHttpClient _httpClient;
    private readonly Lazy<PoolsApi> _pools;
    private readonly Lazy<DevicesApi> _devices;
    private readonly Lazy<AllocationApi> _allocations;
    private readonly Lazy<SessionApi> _sessions;
    private readonly Lazy<ApplicationApi> _applications;

    private bool _disposed;

    internal DeviceParkApiClient(DeviceParkHttpClient httpClient)
    {
        _httpClient = httpClient;
        _pools = new Lazy<PoolsApi>(() => new PoolsApi(_httpClient));
        _devices = new Lazy<DevicesApi>(() => new DevicesApi(_httpClient, _pools.Value));
        _allocations = new Lazy<AllocationApi>(() => new AllocationApi(_httpClient));
        _sessions = new Lazy<SessionApi>(() => new SessionApi(_httpClient));
        _applications = new Lazy<ApplicationApi>(() => new ApplicationApi(_httpClient));
    }

    public static DeviceParkApiClientBuilder Builder() => new DeviceParkApiClientBuilder();

    public DevicesApi Devices => _devices.Value;

    public PoolsApi Pools => _pools.Value;

    public AllocationApi Allocations => _allocations.Value;

    public SessionApi Sessions => _sessions.Value;

    public ApplicationApi Applications => _applications.Value;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _httpClient.Dispose();
    }
}
