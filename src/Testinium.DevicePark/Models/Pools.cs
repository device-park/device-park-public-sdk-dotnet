using System.Collections.Generic;
using System.Runtime.Serialization;
using Testinium.DevicePark.Errors;

namespace Testinium.DevicePark.Models;

public enum PoolFilter
{
    NAME,
    IS_DEFAULT
}

public sealed class PoolFilterRequest : FilterRequest
{
    public PoolFilter Key { get; set; }

    internal override string KeyName => Key.ToString();

    public static PoolFilterRequest Of(PoolFilter key, object? value, SearchOperation operation) =>
        new PoolFilterRequest { Key = key, Value = value, Operation = operation };
}

[DataContract]
public sealed class Pool
{
    [DataMember(Name = "id")]
    public string? Id { get; private set; }

    [DataMember(Name = "name")]
    public string? Name { get; private set; }

    [DataMember(Name = "isDefault")]
    public bool? IsDefault { get; private set; }
}

[DataContract]
public sealed class CreatePoolRequest
{
    [DataMember(Name = "name")]
    public string Name { get; private set; } = string.Empty;

    public sealed class Builder
    {
        private string? _name;

        public Builder Name(string name)
        {
            _name = name;
            return this;
        }

        // DP-PARITY:FROM-NODE  Java'da bu dogrulama build sirasinda degil, PoolsApi.create icinde yapiliyor.
        public CreatePoolRequest Build()
        {
            if (string.IsNullOrWhiteSpace(_name))
            {
                throw new DeviceParkConfigException("name cannot be empty");
            }

            return new CreatePoolRequest { Name = _name! };
        }
    }
}

public sealed class ListPoolsRequest
{
    private readonly List<PoolFilterRequest> _filters = new List<PoolFilterRequest>();

    public IReadOnlyList<PoolFilterRequest> Filters => _filters;

    public Sorting Sorting { get; private set; } = new Sorting();

    public sealed class Builder
    {
        private readonly ListPoolsRequest _request = new ListPoolsRequest();

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

        public Builder AddFilter(PoolFilter key, object? value, SearchOperation operation)
        {
            _request._filters.Add(PoolFilterRequest.Of(key, value, operation));
            return this;
        }

        public Builder Filters(IEnumerable<PoolFilterRequest> filters)
        {
            _request._filters.Clear();
            _request._filters.AddRange(filters);
            return this;
        }

        public ListPoolsRequest Build()
        {
            var built = new ListPoolsRequest { Sorting = _request.Sorting.Copy() };
            built._filters.AddRange(_request._filters);
            return built;
        }
    }
}
