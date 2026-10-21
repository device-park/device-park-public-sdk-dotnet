using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Testinium.DevicePark.Models;

// DP-PARITY:FROM-NODE  Bu dosyadaki tum tipler Java SDK'da yok; cihaz uygulamalari API'si yalnizca Node SDK'da tanimli.
public enum DeviceAppFilter
{
    BUNDLE_IDENTIFIER,
    IS_DEFAULT,
    FILE_KEY
}

// DP-PARITY:FROM-NODE
public sealed class DeviceAppFilterRequest : FilterRequest
{
    public DeviceAppFilter Key { get; set; }

    internal override string KeyName => Key.ToString();

    public static DeviceAppFilterRequest Of(DeviceAppFilter key, object? value, SearchOperation operation) =>
        new DeviceAppFilterRequest { Key = key, Value = value, Operation = operation };
}

// DP-PARITY:FROM-NODE
[DataContract]
public sealed class DeviceApp
{
    [DataMember(Name = "id")]
    public long? Id { get; private set; }

    [DataMember(Name = "bundleIdentifier")]
    public string? BundleIdentifier { get; private set; }

    [DataMember(Name = "installedAt")]
    public string? InstalledAt { get; private set; }

    [DataMember(Name = "updatedAt")]
    public string? UpdatedAt { get; private set; }

    [DataMember(Name = "isDefault")]
    public bool? IsDefault { get; private set; }
}

// DP-PARITY:FROM-NODE
public sealed class ListDeviceAppsRequest
{
    private readonly List<DeviceAppFilterRequest> _filters = new List<DeviceAppFilterRequest>();

    public IReadOnlyList<DeviceAppFilterRequest> Filters => _filters;

    public Sorting Sorting { get; private set; } = new Sorting();

    public sealed class Builder
    {
        private readonly ListDeviceAppsRequest _request = new ListDeviceAppsRequest();

        public Builder Page(int page)
        {
            _request.Sorting.Page = page;
            return this;
        }

        public Builder Size(int size)
        {
            _request.Sorting.Size = size;
            return this;
        }

        public Builder SortBy(string sortBy)
        {
            _request.Sorting.SortBy = sortBy;
            return this;
        }

        public Builder Direction(SortDirection direction)
        {
            _request.Sorting.Direction = direction;
            return this;
        }

        public Builder AddFilter(DeviceAppFilter key, object? value, SearchOperation operation)
        {
            _request._filters.Add(DeviceAppFilterRequest.Of(key, value, operation));
            return this;
        }

        public Builder Filters(IEnumerable<DeviceAppFilterRequest> filters)
        {
            _request._filters.Clear();
            _request._filters.AddRange(filters);
            return this;
        }

        public ListDeviceAppsRequest Build()
        {
            var built = new ListDeviceAppsRequest { Sorting = _request.Sorting.Copy() };
            built._filters.AddRange(_request._filters);
            return built;
        }
    }
}
