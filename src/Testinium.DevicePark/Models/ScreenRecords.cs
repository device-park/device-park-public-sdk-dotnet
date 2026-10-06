using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Testinium.DevicePark.Models;

public enum ScreenRecordFilter
{
    CREATED_AT
}

public sealed class ScreenRecordFilterRequest : FilterRequest
{
    public ScreenRecordFilter Key { get; set; }

    internal override string KeyName => Key.ToString();

    public static ScreenRecordFilterRequest Of(ScreenRecordFilter key, object? value, SearchOperation operation) =>
        new ScreenRecordFilterRequest { Key = key, Value = value, Operation = operation };
}

[DataContract]
public sealed class ScreenRecord
{
    [DataMember(Name = "fileKey")]
    public string? FileKey { get; private set; }

    [DataMember(Name = "filePath")]
    public string? FilePath { get; private set; }

    [DataMember(Name = "downloadUrl")]
    public string? DownloadUrl { get; private set; }

    [DataMember(Name = "createdAt")]
    public string? CreatedAt { get; private set; }

    [DataMember(Name = "updatedAt")]
    public string? UpdatedAt { get; private set; }

    [DataMember(Name = "duration")]
    public double? Duration { get; private set; }
}

public sealed class ScreenRecordPaginationRequest
{
    private readonly List<ScreenRecordFilterRequest> _filters = new List<ScreenRecordFilterRequest>();

    public IReadOnlyList<ScreenRecordFilterRequest> Filters => _filters;

    public Sorting Sorting { get; private set; } = new Sorting();

    public sealed class Builder
    {
        private readonly ScreenRecordPaginationRequest _request = new ScreenRecordPaginationRequest();

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

        public Builder AddFilter(ScreenRecordFilter key, object? value, SearchOperation operation)
        {
            _request._filters.Add(ScreenRecordFilterRequest.Of(key, value, operation));
            return this;
        }

        public Builder Filters(IEnumerable<ScreenRecordFilterRequest> filters)
        {
            _request._filters.Clear();
            _request._filters.AddRange(filters);
            return this;
        }

        public ScreenRecordPaginationRequest Build()
        {
            var built = new ScreenRecordPaginationRequest { Sorting = _request.Sorting.Copy() };
            built._filters.AddRange(_request._filters);
            return built;
        }
    }
}
