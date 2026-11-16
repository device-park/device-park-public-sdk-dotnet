using Testinium.DevicePark.Apis;
using Testinium.DevicePark.Errors;
using Xunit;

namespace Testinium.DevicePark.Tests;

public sealed class StreamUrlsTests
{
    [Fact]
    public void AppendsTokenAsQueryParameter()
    {
        var url = StreamUrls.WithAccessToken(
            "wss://stream.example/ws/v2/devicepark/sessions/12345/stream",
            "abc.def");

        Assert.Equal(
            "wss://stream.example/ws/v2/devicepark/sessions/12345/stream?token=abc.def",
            url);
    }

    [Fact]
    public void EncodesTokenAndKeepsExistingQuery()
    {
        var url = StreamUrls.WithAccessToken("wss://stream.example/stream?session=1", "a+b/c=");

        Assert.Equal("wss://stream.example/stream?session=1&token=a%2Bb%2Fc%3D", url);
    }

    [Fact]
    public void RejectsBlankUrlAndBlankToken()
    {
        Assert.Throws<DeviceParkConfigException>(() => StreamUrls.WithAccessToken("  ", "token"));
        Assert.Throws<DeviceParkConfigException>(
            () => StreamUrls.WithAccessToken("wss://stream.example/stream", " "));
    }
}
