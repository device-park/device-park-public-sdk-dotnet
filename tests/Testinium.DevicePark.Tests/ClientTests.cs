using System;
using System.Net;
using System.Net.Http;
using System.Text;
using Testinium.DevicePark.Errors;
using Xunit;

namespace Testinium.DevicePark.Tests;

public sealed class ClientTests
{
    [Fact]
    public void BuildRejectsMissingUrl()
    {
        var builder = DeviceParkApiClient.Builder()
            .Credentials(Credentials.Of("client-id", "client-secret"));

        var error = Assert.Throws<DeviceParkConfigException>(() => builder.Build());
        Assert.Equal("url cannot be empty", error.Message);
    }

    [Fact]
    public void BuildRejectsMissingCredentials()
    {
        var builder = DeviceParkApiClient.Builder().Url("https://devicepark.testinium.io");

        var error = Assert.Throws<DeviceParkConfigException>(() => builder.Build());
        Assert.Equal("credentials are required", error.Message);
    }

    [Fact]
    public void CredentialsRejectBlankValues()
    {
        Assert.Throws<DeviceParkConfigException>(() => Credentials.Of("   ", "secret"));
        Assert.Throws<DeviceParkConfigException>(() => Credentials.Of("id", "   "));
    }

    [Fact]
    public void CredentialsFromEnvironmentReadsStandardVariables()
    {
        Environment.SetEnvironmentVariable("DEVICEPARK_CLIENT_ID", "env-id");
        Environment.SetEnvironmentVariable("DEVICEPARK_CLIENT_SECRET", "env-secret");

        try
        {
            var credentials = Credentials.FromEnvironment();

            Assert.Equal("env-id", credentials.ClientId);
            Assert.Equal("env-secret", credentials.ClientSecret);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DEVICEPARK_CLIENT_ID", null);
            Environment.SetEnvironmentVariable("DEVICEPARK_CLIENT_SECRET", null);
        }
    }

    [Fact]
    public void CredentialsFromEnvironmentFailsWhenUnset()
    {
        Environment.SetEnvironmentVariable("DEVICEPARK_CLIENT_ID", null);
        Environment.SetEnvironmentVariable("DEVICEPARK_CLIENT_SECRET", null);

        Assert.Throws<DeviceParkConfigException>(() => Credentials.FromEnvironment());
    }

    [Fact]
    public void ServiceAccessorsAreMemoized()
    {
        using var client = TestClient.Create(StubHandler.Json("{}"));

        Assert.Same(client.Devices, client.Devices);
        Assert.Same(client.Pools, client.Pools);
        Assert.Same(client.Allocations, client.Allocations);
        Assert.Same(client.Sessions, client.Sessions);
        Assert.Same(client.Applications, client.Applications);
    }

    [Fact]
    public async System.Threading.Tasks.Task NonSuccessResponseBecomesTypedHttpException()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Device not found", Encoding.UTF8, "text/plain")
        });

        using var client = TestClient.Create(handler);

        var error = await Assert.ThrowsAsync<DeviceParkHttpException>(
            () => client.Devices.GetAsync("UNKNOWN-SERIAL"));

        Assert.Equal(404, error.Status);
        Assert.Equal("Device not found", error.Body);
    }

    [Fact]
    public async System.Threading.Tasks.Task UnauthorizedResponseRefreshesTokenAndRetriesOnce()
    {
        var handler = new StubHandler((_, attempt) => attempt == 1
            ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("expired", Encoding.UTF8, "text/plain")
            }
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Payloads.EmptyPage, Encoding.UTF8, "application/json")
            });

        using var client = TestClient.Create(handler);

        var page = await client.Devices.ListAsync();

        Assert.True(page.IsEmpty);
        Assert.Equal(2, handler.ApiRequests.Count);
        Assert.Equal(2, handler.TokenRequestCount);
        Assert.Equal("Bearer token-1", handler.ApiRequests[0].Headers["Authorization"]);
        Assert.Equal("Bearer token-2", handler.ApiRequests[1].Headers["Authorization"]);
    }

    [Fact]
    public async System.Threading.Tasks.Task RepeatedUnauthorizedResponseSurfacesTheError()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("denied", Encoding.UTF8, "text/plain")
        });

        using var client = TestClient.Create(handler);

        var error = await Assert.ThrowsAsync<DeviceParkHttpException>(() => client.Devices.ListAsync());

        Assert.Equal(401, error.Status);
        Assert.Equal(2, handler.ApiRequests.Count);
    }

    [Fact]
    public async System.Threading.Tasks.Task TokenIsFetchedOnceAndReusedAcrossCalls()
    {
        var handler = StubHandler.Json(Payloads.EmptyPage);

        using var client = TestClient.Create(handler);

        await client.Devices.ListAsync();
        await client.Pools.ListAsync();
        await client.Sessions.ListAsync();

        Assert.Equal(1, handler.TokenRequestCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task CustomHeadersAreSentWithEveryRequest()
    {
        var handler = StubHandler.Json(Payloads.EmptyPage);

        using var client = DeviceParkApiClient.Builder()
            .Url("https://devicepark.testinium.io")
            .Credentials(Credentials.Of("client-id", "client-secret"))
            .AddHeader("x-trace-id", "abc-123")
            .HttpMessageHandler(handler)
            .Build();

        await client.Devices.ListAsync();

        Assert.Equal("abc-123", handler.ApiRequests[0].Headers["x-trace-id"]);
    }

    [Fact]
    public async System.Threading.Tasks.Task BaseUrlWithTrailingSlashDoesNotDoubleUpSeparators()
    {
        var handler = StubHandler.Json(Payloads.EmptyPage);

        using var client = DeviceParkApiClient.Builder()
            .Url("https://devicepark.testinium.io/")
            .Credentials(Credentials.Of("client-id", "client-secret"))
            .HttpMessageHandler(handler)
            .Build();

        await client.Devices.ListAsync();

        Assert.Equal("/management/api/v2/public/devices", handler.ApiRequests[0].Uri.AbsolutePath);
    }
}
