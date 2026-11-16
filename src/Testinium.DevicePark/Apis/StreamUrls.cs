using System;
using Testinium.DevicePark.Errors;

namespace Testinium.DevicePark.Apis;

// Joins a token-less screen-stream URL with the caller's current access token.
public static class StreamUrls
{
    public static string WithAccessToken(string streamUrl, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(streamUrl))
        {
            throw new DeviceParkConfigException("stream url cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new DeviceParkConfigException("access token cannot be empty");
        }

        var separator = streamUrl.IndexOf('?') >= 0 ? "&" : "?";
        return streamUrl + separator + "token=" + Uri.EscapeDataString(accessToken);
    }
}
