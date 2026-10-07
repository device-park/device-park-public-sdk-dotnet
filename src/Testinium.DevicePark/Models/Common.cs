using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Testinium.DevicePark.Models;

public enum SortDirection
{
    ASC,
    DESC
}

public enum SearchOperation
{
    EQUAL,
    NOT_EQUAL,
    GREATER_THAN,
    LESS_THAN
}

public sealed class Sorting
{
    public int Page { get; set; }

    public int Size { get; set; } = 20;

    public string SortBy { get; set; } = "ID";

    public SortDirection Direction { get; set; } = SortDirection.DESC;

    internal Sorting Copy() => new Sorting
    {
        Page = Page,
        Size = Size,
        SortBy = SortBy,
        Direction = Direction
    };
}

public abstract class FilterRequest
{
    public object? Value { get; set; }

    public SearchOperation Operation { get; set; }

    internal abstract string KeyName { get; }
}

[DataContract]
public sealed class PageDto<T>
{
    [DataMember(Name = "size")]
    public int Size { get; private set; }

    [DataMember(Name = "page")]
    public int Page { get; private set; }

    [DataMember(Name = "totalPages")]
    public int TotalPages { get; private set; }

    [DataMember(Name = "totalElements")]
    public long TotalElements { get; private set; }

    [DataMember(Name = "data")]
    private List<T>? DataValue { get; set; }

    public IReadOnlyList<T> Data => DataValue ?? new List<T>();

    // DP-PARITY:JAVA-ONLY  Node SDK'nin PageDto'sunda bu yardimcilar yok.
    public bool IsLast => Page >= TotalPages - 1;

    // DP-PARITY:JAVA-ONLY  Node SDK'nin PageDto'sunda bu yardimcilar yok.
    public bool IsEmpty => Data.Count == 0;
}
