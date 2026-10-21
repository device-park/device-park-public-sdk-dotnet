using System;
using Testinium.DevicePark.Errors;

namespace Testinium.DevicePark;

public sealed class Credentials
{
    private Credentials(string clientId, string clientSecret)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
    }

    public string ClientId { get; }

    public string ClientSecret { get; }

    public static Credentials Of(string clientId, string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new DeviceParkConfigException("clientId cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new DeviceParkConfigException("clientSecret cannot be empty");
        }

        return new Credentials(clientId, clientSecret);
    }

    // DP-PARITY:FROM-NODE  Java SDK'da ortam degiskeni destegi hic yok; README'si anlatsa da kodda getenv cagrisi bulunmuyor.
    public static Credentials FromEnvironment() => Of(
        Environment.GetEnvironmentVariable("DEVICEPARK_CLIENT_ID") ?? string.Empty,
        Environment.GetEnvironmentVariable("DEVICEPARK_CLIENT_SECRET") ?? string.Empty);
}
