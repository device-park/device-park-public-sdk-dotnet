using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Testinium.DevicePark.Errors;

namespace Testinium.DevicePark.Models;

public enum RemoveAppSelection
{
    NO_REMOVE,
    REMOVE_WITHOUT_IS_DEFAULT_APPS
}

public enum AllocationFilter
{
    ALLOCATION
}

public sealed class AllocationFilterRequest : FilterRequest
{
    public AllocationFilter Key { get; set; }

    internal override string KeyName => Key.ToString();

    public static AllocationFilterRequest Of(AllocationFilter key, object? value, SearchOperation operation) =>
        new AllocationFilterRequest { Key = key, Value = value, Operation = operation };
}

[DataContract]
public sealed class Allocation
{
    [DataMember(Name = "allocationId")]
    public string? AllocationId { get; private set; }

    [DataMember(Name = "deviceSerial")]
    public string? DeviceSerial { get; private set; }

    [DataMember(Name = "requestId")]
    public string? RequestId { get; private set; }

    [DataMember(Name = "position")]
    public int? Position { get; private set; }

    [DataMember(Name = "expiresAt")]
    public string? ExpiresAt { get; private set; }
}

[DataContract]
public sealed class DeviceAllocationRequest
{
    [DataMember(Name = "serial", EmitDefaultValue = false)]
    private string? SerialValue { get; set; }

    [DataMember(Name = "manufacturer", EmitDefaultValue = false)]
    private string? ManufacturerValue { get; set; }

    [DataMember(Name = "model", EmitDefaultValue = false)]
    private string? ModelValue { get; set; }

    [DataMember(Name = "platform", EmitDefaultValue = false)]
    private string? PlatformValue { get; set; }

    [DataMember(Name = "platformVersion", EmitDefaultValue = false)]
    private string? PlatformVersionValue { get; set; }

    [DataMember(Name = "devicePoolId", EmitDefaultValue = false)]
    private string? DevicePoolIdValue { get; set; }

    [DataMember(Name = "priority")]
    private int PriorityValue { get; set; } = 3;

    [DataMember(Name = "removeApps")]
    private string RemoveAppsValue { get; set; } = RemoveAppSelection.NO_REMOVE.ToString();

    public string? Serial => SerialValue;

    public string? Manufacturer => ManufacturerValue;

    public string? Model => ModelValue;

    public string? Platform => PlatformValue;

    public string? PlatformVersion => PlatformVersionValue;

    public string? DevicePoolId => DevicePoolIdValue;

    public int Priority => PriorityValue;

    public RemoveAppSelection RemoveApps =>
        (RemoveAppSelection)Enum.Parse(typeof(RemoveAppSelection), RemoveAppsValue);

    public sealed class Builder
    {
        private readonly DeviceAllocationRequest _request = new DeviceAllocationRequest();

        public Builder Serial(string serial)
        {
            _request.SerialValue = serial;
            return this;
        }

        public Builder Manufacturer(string manufacturer)
        {
            _request.ManufacturerValue = manufacturer;
            return this;
        }

        public Builder Model(string model)
        {
            _request.ModelValue = model;
            return this;
        }

        public Builder Platform(string platform)
        {
            _request.PlatformValue = platform;
            return this;
        }

        public Builder PlatformVersion(string platformVersion)
        {
            _request.PlatformVersionValue = platformVersion;
            return this;
        }

        public Builder DevicePoolId(string devicePoolId)
        {
            _request.DevicePoolIdValue = devicePoolId;
            return this;
        }

        public Builder Priority(int priority)
        {
            if (priority < 1 || priority > 5)
            {
                throw new DeviceParkConfigException("priority must be between 1 and 5");
            }

            _request.PriorityValue = priority;
            return this;
        }

        public Builder RemoveApps(RemoveAppSelection removeApps)
        {
            _request.RemoveAppsValue = removeApps.ToString();
            return this;
        }

        public DeviceAllocationRequest Build() => new DeviceAllocationRequest
        {
            SerialValue = _request.SerialValue,
            ManufacturerValue = _request.ManufacturerValue,
            ModelValue = _request.ModelValue,
            PlatformValue = _request.PlatformValue,
            PlatformVersionValue = _request.PlatformVersionValue,
            DevicePoolIdValue = _request.DevicePoolIdValue,
            PriorityValue = _request.PriorityValue,
            RemoveAppsValue = _request.RemoveAppsValue
        };
    }
}

public sealed class AllocationSearchRequest
{
    private readonly List<AllocationFilterRequest> _filters = new List<AllocationFilterRequest>();

    public IReadOnlyList<AllocationFilterRequest> Filters => _filters;

    public Sorting Sorting { get; private set; } = new Sorting();

    public sealed class Builder
    {
        private readonly AllocationSearchRequest _request = new AllocationSearchRequest();

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

        public Builder AddFilter(AllocationFilter key, object? value, SearchOperation operation)
        {
            _request._filters.Add(AllocationFilterRequest.Of(key, value, operation));
            return this;
        }

        public Builder Filters(IEnumerable<AllocationFilterRequest> filters)
        {
            _request._filters.Clear();
            _request._filters.AddRange(filters);
            return this;
        }

        public AllocationSearchRequest Build()
        {
            var built = new AllocationSearchRequest { Sorting = _request.Sorting.Copy() };
            built._filters.AddRange(_request._filters);
            return built;
        }
    }
}
