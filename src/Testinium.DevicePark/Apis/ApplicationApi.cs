using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Testinium.DevicePark.Errors;
using Testinium.DevicePark.Internal;
using Testinium.DevicePark.Models;

namespace Testinium.DevicePark.Apis;

public sealed class ApplicationApi
{
    private const string ApplicationPath = "/storage/api/v1/public/applications";

    private readonly DeviceParkHttpClient _httpClient;

    internal ApplicationApi(DeviceParkHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PageDto<Application>> ListAsync(
        ApplicationPaginationRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var effective = request ?? new ApplicationPaginationRequest.Builder().Build();
        var response = await _httpClient
            .GetAsync(ApplicationPath, Query.Pagination(effective.Sorting, effective.Filters), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<PageDto<Application>>(response);
    }

    public async Task<Application> UploadAsync(
        Stream fileStream,
        string fileName,
        string version,
        bool imageInjection = false,
        CancellationToken cancellationToken = default)
    {
        if (fileStream is null)
        {
            throw new DeviceParkConfigException("fileStream cannot be null");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DeviceParkConfigException("fileName cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new DeviceParkConfigException("version cannot be empty");
        }

        if (version.Length > 50)
        {
            throw new DeviceParkConfigException("version length must be at most 50 characters");
        }

        var headers = new Dictionary<string, string>
        {
            ["file-name"] = fileName,
            ["version"] = version,
            ["image-injection"] = imageInjection ? "true" : "false"
        };

        var response = await _httpClient
            .PostStreamAsync(ApplicationPath, fileStream, headers, cancellationToken)
            .ConfigureAwait(false);

        return JsonMapper.FromJson<Application>(response);
    }

    public async Task DeleteAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
        {
            throw new DeviceParkConfigException("fileKey cannot be empty");
        }

        await _httpClient
            .DeleteAsync(ApplicationPath + "/" + Uri.EscapeDataString(fileKey), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
