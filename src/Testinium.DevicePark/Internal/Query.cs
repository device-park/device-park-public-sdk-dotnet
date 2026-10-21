using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Testinium.DevicePark.Models;

namespace Testinium.DevicePark.Internal;

internal static class Query
{
    internal static List<KeyValuePair<string, object?>> Pagination(
        Sorting sorting,
        IEnumerable<FilterRequest>? filters)
    {
        var parameters = new List<KeyValuePair<string, object?>>
        {
            new KeyValuePair<string, object?>("sorting.page", sorting.Page),
            new KeyValuePair<string, object?>("sorting.size", sorting.Size),
            new KeyValuePair<string, object?>("sorting.sortBy", sorting.SortBy),
            new KeyValuePair<string, object?>("sorting.direction", sorting.Direction)
        };

        if (filters is null)
        {
            return parameters;
        }

        var index = 0;
        foreach (var filter in filters)
        {
            if (filter is null)
            {
                continue;
            }

            var prefix = "filters[" + index.ToString(CultureInfo.InvariantCulture) + "].";
            parameters.Add(new KeyValuePair<string, object?>(prefix + "key", filter.KeyName));
            parameters.Add(new KeyValuePair<string, object?>(prefix + "value", filter.Value));
            parameters.Add(new KeyValuePair<string, object?>(prefix + "operation", filter.Operation));
            index++;
        }

        return parameters;
    }

    internal static string BuildUri(
        string baseUrl,
        string path,
        IEnumerable<KeyValuePair<string, object?>>? query = null)
    {
        var url = Combine(baseUrl, path);

        if (query is null)
        {
            return url;
        }

        var builder = new StringBuilder();
        foreach (var parameter in query)
        {
            if (parameter.Value is null)
            {
                continue;
            }

            if (parameter.Value is string || parameter.Value is not IEnumerable enumerable)
            {
                Append(builder, parameter.Key, parameter.Value);
                continue;
            }

            foreach (var item in enumerable)
            {
                Append(builder, parameter.Key, item);
            }
        }

        return builder.Length == 0 ? url : url + "?" + builder;
    }

    private static string Combine(string baseUrl, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return baseUrl;
        }

        var baseEndsWithSlash = baseUrl.EndsWith("/", StringComparison.Ordinal);
        var pathStartsWithSlash = path.StartsWith("/", StringComparison.Ordinal);

        if (baseEndsWithSlash && pathStartsWithSlash)
        {
            return baseUrl + path.Substring(1);
        }

        if (!baseEndsWithSlash && !pathStartsWithSlash)
        {
            return baseUrl + "/" + path;
        }

        return baseUrl + path;
    }

    private static void Append(StringBuilder builder, string key, object? value)
    {
        var scalar = Scalar(value);
        if (scalar is null)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('&');
        }

        builder.Append(Uri.EscapeDataString(key));
        builder.Append('=');
        builder.Append(Uri.EscapeDataString(scalar));
    }

    private static string? Scalar(object? value) => value switch
    {
        null => null,
        bool flag => flag ? "true" : "false",
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };
}
