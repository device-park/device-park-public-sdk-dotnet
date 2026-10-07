using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Testinium.DevicePark.Models;

public enum ApplicationFilter
{
    FILE_KEY,
    FILE_PATH,
    VERSION,
    REVISION
}

public sealed class ApplicationFilterRequest : FilterRequest
{
    public ApplicationFilter Key { get; set; }

    internal override string KeyName => Key.ToString();

    public static ApplicationFilterRequest Of(ApplicationFilter key, object? value, SearchOperation operation) =>
        new ApplicationFilterRequest { Key = key, Value = value, Operation = operation };
}

[DataContract]
public sealed class Application
{
    [DataMember(Name = "revision")]
    public long? Revision { get; private set; }

    [DataMember(Name = "size_in_bytes")]
    public long? SizeInBytes { get; private set; }

    [DataMember(Name = "version")]
    public string? Version { get; private set; }

    [DataMember(Name = "fileKey")]
    public string? FileKey { get; private set; }

    [DataMember(Name = "filePath")]
    public string? FilePath { get; private set; }

    [DataMember(Name = "downloadUrl")]
    public string? DownloadUrl { get; private set; }

    [DataMember(Name = "createdAt")]
    public string? CreatedAt { get; private set; }
}

public sealed class ApplicationPaginationRequest
{
    private readonly List<ApplicationFilterRequest> _filters = new List<ApplicationFilterRequest>();

    public IReadOnlyList<ApplicationFilterRequest> Filters => _filters;

    public Sorting Sorting { get; private set; } = new Sorting();

    public sealed class Builder
    {
        private readonly ApplicationPaginationRequest _request = new ApplicationPaginationRequest();

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

        public Builder AddFilter(ApplicationFilter key, object? value, SearchOperation operation)
        {
            _request._filters.Add(ApplicationFilterRequest.Of(key, value, operation));
            return this;
        }

        public Builder Filters(IEnumerable<ApplicationFilterRequest> filters)
        {
            _request._filters.Clear();
            _request._filters.AddRange(filters);
            return this;
        }

        public ApplicationPaginationRequest Build()
        {
            var built = new ApplicationPaginationRequest { Sorting = _request.Sorting.Copy() };
            built._filters.AddRange(_request._filters);
            return built;
        }
    }
}
