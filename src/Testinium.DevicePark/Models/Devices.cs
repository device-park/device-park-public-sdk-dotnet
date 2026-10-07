using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Testinium.DevicePark.Models;

// DP-PARITY:JAVA-DEAD  Java DeviceFilter enum'u her sabit icin bir getDbField() degeri tasiyor ("devicePools.id" gibi)
// DP-PARITY:JAVA-DEAD  ama bu deger Java icinde hicbir yerde okunmuyor; query'ye enum adi gidiyor. dbField eslemesi tasinmadi.
public enum DeviceFilter
{
    POOL_ID,
    SERIAL_NUMBER,
    MARKETING_NAME,
    MANUFACTURER,
    MODEL_NAME,
    PLATFORM,

    // DP-PARITY:JAVA-ONLY  Node SDK'da bu sabit OS_VERSION adiyla ve "osVersion" degeriyle duruyor.
    PLATFORM_VERSION,

    TAGS,
    STATE
}

public sealed class DeviceFilterRequest : FilterRequest
{
    public DeviceFilter Key { get; set; }

    internal override string KeyName => Key.ToString();

    public static DeviceFilterRequest Of(DeviceFilter key, object? value, SearchOperation operation) =>
        new DeviceFilterRequest { Key = key, Value = value, Operation = operation };
}

[DataContract]
public sealed class Device
{
    [DataMember(Name = "id")]
    public long? Id { get; private set; }

    [DataMember(Name = "serial")]
    public string? Serial { get; private set; }

    [DataMember(Name = "marketName")]
    public string? MarketName { get; private set; }

    [DataMember(Name = "model")]
    public string? Model { get; private set; }

    [DataMember(Name = "manufacturer")]
    public string? Manufacturer { get; private set; }

    [DataMember(Name = "platform")]
    public string? Platform { get; private set; }

    [DataMember(Name = "platformVersion")]
    public string? PlatformVersion { get; private set; }

    [DataMember(Name = "version")]
    public string? Version { get; private set; }

    [DataMember(Name = "state")]
    public string? State { get; private set; }

    [DataMember(Name = "isSimulator")]
    public bool? IsSimulator { get; private set; }

    [DataMember(Name = "isPublic")]
    public bool? IsPublic { get; private set; }
}

public sealed class ListDevicesRequest
{
    private readonly List<DeviceFilterRequest> _filters = new List<DeviceFilterRequest>();

    public IReadOnlyList<DeviceFilterRequest> Filters => _filters;

    public Sorting Sorting { get; private set; } = new Sorting();

    public sealed class Builder
    {
        private readonly ListDevicesRequest _request = new ListDevicesRequest();

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

        public Builder AddFilter(DeviceFilter key, object? value, SearchOperation operation)
        {
            _request._filters.Add(DeviceFilterRequest.Of(key, value, operation));
            return this;
        }

        public Builder Filters(IEnumerable<DeviceFilterRequest> filters)
        {
            _request._filters.Clear();
            _request._filters.AddRange(filters);
            return this;
        }

        // DP-PARITY:FROM-NODE  Java builder'i her Build() cagrisinda ayni mutable nesneyi dondurur; Node savunmaci kopya dondurur.
        public ListDevicesRequest Build()
        {
            var built = new ListDevicesRequest { Sorting = _request.Sorting.Copy() };
            built._filters.AddRange(_request._filters);
            return built;
        }
    }
}
