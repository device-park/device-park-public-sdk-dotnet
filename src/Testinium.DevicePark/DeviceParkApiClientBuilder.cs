using System.Collections.Generic;
using System.Net.Http;
using Testinium.DevicePark.Errors;
using Testinium.DevicePark.Internal;

namespace Testinium.DevicePark;

// DP-PARITY:JAVA-DEAD  Java'da builder varsayilani 60 saniye olmasina ragmen DeviceParkHttpClient null timeout gelirse
// DP-PARITY:JAVA-DEAD  sessizce 20 saniyeye dusuyor. Bu ikinci varsayilan tasinmadi; tek varsayilan 60 saniye.
public sealed class DeviceParkApiClientBuilder
{
    private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();

    private string? _url;
    private Credentials? _credentials;
    private int _timeoutSeconds = 60;
    private HttpMessageHandler? _handler;

    public DeviceParkApiClientBuilder Url(string url)
    {
        _url = url;
        return this;
    }

    public DeviceParkApiClientBuilder Credentials(Credentials credentials)
    {
        _credentials = credentials;
        return this;
    }

    public DeviceParkApiClientBuilder Timeout(int timeoutSeconds)
    {
        _timeoutSeconds = timeoutSeconds;
        return this;
    }

    // DP-PARITY:FROM-NODE  Java builder'inda her istege eklenecek ek header tanimlama imkani yok.
    public DeviceParkApiClientBuilder AddHeader(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    // DP-PARITY:FROM-NODE  Node builder'indaki fetchImplementation karsiligi; testlerde transport degistirmeye yarar.
    public DeviceParkApiClientBuilder HttpMessageHandler(HttpMessageHandler handler)
    {
        _handler = handler;
        return this;
    }

    public DeviceParkApiClient Build()
    {
        if (string.IsNullOrWhiteSpace(_url))
        {
            throw new DeviceParkConfigException("url cannot be empty");
        }

        if (_credentials is null)
        {
            throw new DeviceParkConfigException("credentials are required");
        }

        if (_timeoutSeconds <= 0)
        {
            throw new DeviceParkConfigException("timeout must be greater than zero");
        }

        var httpClient = new DeviceParkHttpClient(
            _url!.Trim(),
            _timeoutSeconds,
            _credentials,
            _handler,
            new Dictionary<string, string>(_headers));

        return new DeviceParkApiClient(httpClient);
    }
}
