using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Testinium.DevicePark.Errors;
using Testinium.DevicePark.Models;
using Xunit;

namespace Testinium.DevicePark.Tests;

public sealed class ApiTests
{
    [Fact]
    public async Task ListDevicesUsesV2PathWithSortingAndFilterQuery()
    {
        var handler = StubHandler.Json(Payloads.DevicePage);
        using var client = TestClient.Create(handler);

        var page = await client.Devices.ListAsync(new ListDevicesRequest.Builder()
            .Page(2)
            .Size(50)
            .SortBy("SERIAL")
            .Direction(SortDirection.ASC)
            .AddFilter(DeviceFilter.PLATFORM, "android", SearchOperation.EQUAL)
            .Build());

        var uri = handler.ApiRequests[0].Uri;
        var query = HttpUtility.ParseQueryString(uri.Query);

        Assert.Equal("/management/api/v2/public/devices", uri.AbsolutePath);
        Assert.Equal("2", query["sorting.page"]);
        Assert.Equal("50", query["sorting.size"]);
        Assert.Equal("SERIAL", query["sorting.sortBy"]);
        Assert.Equal("ASC", query["sorting.direction"]);
        Assert.Equal("PLATFORM", query["filters[0].key"]);
        Assert.Equal("android", query["filters[0].value"]);
        Assert.Equal("EQUAL", query["filters[0].operation"]);

        Assert.Equal(1, page.TotalElements);
        Assert.Equal("ABC-1", page.Data[0].Serial);
        Assert.Equal("15", page.Data[0].PlatformVersion);
        Assert.False(page.Data[0].IsSimulator);
        Assert.True(page.IsLast);
    }

    [Fact]
    public async Task ListDevicesUsesDefaultPaginationWhenRequestOmitted()
    {
        var handler = StubHandler.Json(Payloads.EmptyPage);
        using var client = TestClient.Create(handler);

        await client.Devices.ListAsync();

        var query = HttpUtility.ParseQueryString(handler.ApiRequests[0].Uri.Query);

        Assert.Equal("0", query["sorting.page"]);
        Assert.Equal("20", query["sorting.size"]);
        Assert.Equal("ID", query["sorting.sortBy"]);
        Assert.Equal("DESC", query["sorting.direction"]);
        Assert.Null(query["filters[0].key"]);
    }

    [Fact]
    public async Task BooleanFilterValuesAreSentLowercase()
    {
        var handler = StubHandler.Json(Payloads.EmptyPage);
        using var client = TestClient.Create(handler);

        await client.Devices.AppsAsync("SERIAL-1", new ListDeviceAppsRequest.Builder()
            .AddFilter(DeviceAppFilter.IS_DEFAULT, true, SearchOperation.EQUAL)
            .Build());

        var query = HttpUtility.ParseQueryString(handler.ApiRequests[0].Uri.Query);

        Assert.Equal("IS_DEFAULT", query["filters[0].key"]);
        Assert.Equal("true", query["filters[0].value"]);
    }

    [Fact]
    public async Task GetDeviceUsesV1PathAndEscapesTheSerial()
    {
        var handler = StubHandler.Json("{\"id\":7,\"serial\":\"serial/with space\"}");
        using var client = TestClient.Create(handler);

        var device = await client.Devices.GetAsync("serial/with space");

        Assert.Equal(
            "/management/api/v1/public/devices/serial%2Fwith%20space",
            handler.ApiRequests[0].Uri.AbsolutePath);
        Assert.Equal(7, device.Id);
    }

    [Fact]
    public async Task ListDeviceAppsUsesV1SubresourcePath()
    {
        var handler = StubHandler.Json(Payloads.EmptyPage);
        using var client = TestClient.Create(handler);

        await client.Devices.AppsAsync("ABC-1");

        Assert.Equal(
            "/management/api/v1/public/devices/ABC-1/apps",
            handler.ApiRequests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task EmptySerialIsRejectedBeforeAnyRequest()
    {
        var handler = StubHandler.Json(Payloads.EmptyPage);
        using var client = TestClient.Create(handler);

        await Assert.ThrowsAsync<DeviceParkConfigException>(() => client.Devices.GetAsync("   "));
        await Assert.ThrowsAsync<DeviceParkConfigException>(() => client.Devices.AppsAsync("   "));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ListByDefaultPoolSendsIsDefaultFilterAndKeepsRequestedPaging()
    {
        var handler = StubHandler.Json(Payloads.DefaultPoolPage);
        using var client = TestClient.Create(handler);

        var page = await client.Pools.ListByDefaultPoolAsync(new ListPoolsRequest.Builder()
            .Page(2)
            .Size(10)
            .Build());

        var query = HttpUtility.ParseQueryString(handler.ApiRequests[0].Uri.Query);

        Assert.Equal("/management/api/v1/public/pools", handler.ApiRequests[0].Uri.AbsolutePath);
        Assert.Equal("2", query["sorting.page"]);
        Assert.Equal("10", query["sorting.size"]);
        Assert.Equal("IS_DEFAULT", query["filters[0].key"]);
        Assert.Equal("true", query["filters[0].value"]);
        Assert.Equal("EQUAL", query["filters[0].operation"]);
        Assert.True(page.Data[0].IsDefault);
    }

    [Fact]
    public async Task GetDefaultPoolReturnsTheFirstMatch()
    {
        var handler = StubHandler.Json(Payloads.DefaultPoolPage);
        using var client = TestClient.Create(handler);

        var pool = await client.Pools.GetDefaultPoolAsync();

        Assert.Equal("default-pool", pool.Id);
    }

    [Fact]
    public async Task GetDefaultPoolFailsWhenNoDefaultPoolExists()
    {
        var handler = StubHandler.Json(Payloads.EmptyPage);
        using var client = TestClient.Create(handler);

        var error = await Assert.ThrowsAsync<DeviceParkException>(() => client.Pools.GetDefaultPoolAsync());

        Assert.Equal("Default pool not found", error.Message);
    }

    [Fact]
    public async Task ListDevicesByDefaultPoolResolvesThePoolThenFiltersOnIt()
    {
        var handler = new StubHandler((request, attempt) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                attempt == 1 ? Payloads.DefaultPoolPage : Payloads.DevicePage,
                Encoding.UTF8,
                "application/json")
        });

        using var client = TestClient.Create(handler);

        await client.Devices.ListByDefaultPoolAsync(new ListDevicesRequest.Builder()
            .Size(5)
            .AddFilter(DeviceFilter.POOL_ID, "ignored-pool", SearchOperation.EQUAL)
            .AddFilter(DeviceFilter.STATE, "HEALTHY", SearchOperation.EQUAL)
            .Build());

        Assert.Equal(2, handler.ApiRequests.Count);
        Assert.Equal("/management/api/v1/public/pools", handler.ApiRequests[0].Uri.AbsolutePath);

        var query = HttpUtility.ParseQueryString(handler.ApiRequests[1].Uri.Query);

        Assert.Equal("/management/api/v2/public/devices", handler.ApiRequests[1].Uri.AbsolutePath);
        Assert.Equal("5", query["sorting.size"]);
        Assert.Equal("POOL_ID", query["filters[0].key"]);
        Assert.Equal("default-pool", query["filters[0].value"]);
        Assert.Equal("STATE", query["filters[1].key"]);
        Assert.Equal("HEALTHY", query["filters[1].value"]);
        Assert.Null(query["filters[2].key"]);
    }

    [Fact]
    public async Task PoolDeviceMembershipRepeatsTheSerialsQueryParameter()
    {
        var handler = StubHandler.Json("[\"SERIAL-1\",\"SERIAL-2\"]");
        using var client = TestClient.Create(handler);

        var added = await client.Pools.AddDevicesAsync("pool-123", new[] { "SERIAL-1", "SERIAL-2" });
        await client.Pools.RemoveDevicesAsync("pool-123", new[] { "SERIAL-1" });

        Assert.Equal(new[] { "SERIAL-1", "SERIAL-2" }, added);
        Assert.Equal(HttpMethod.Post, handler.ApiRequests[0].Method);
        Assert.Equal(
            new[] { "SERIAL-1", "SERIAL-2" },
            HttpUtility.ParseQueryString(handler.ApiRequests[0].Uri.Query).GetValues("serials"));
        Assert.Equal(HttpMethod.Delete, handler.ApiRequests[1].Method);
        Assert.Equal(
            new[] { "SERIAL-1" },
            HttpUtility.ParseQueryString(handler.ApiRequests[1].Uri.Query).GetValues("serials"));
    }

    [Fact]
    public async Task PoolMutationInputsAreValidated()
    {
        var handler = StubHandler.Json("[]");
        using var client = TestClient.Create(handler);

        await Assert.ThrowsAsync<DeviceParkConfigException>(() => client.Pools.DeleteAsync(" "));
        await Assert.ThrowsAsync<DeviceParkConfigException>(
            () => client.Pools.AddDevicesAsync("pool-123", Array.Empty<string>()));
        await Assert.ThrowsAsync<DeviceParkConfigException>(
            () => client.Pools.RemoveDevicesAsync(" ", new[] { "SERIAL-1" }));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task CreatePoolSendsOnlyTheNameField()
    {
        var handler = StubHandler.Json("{\"id\":\"pool-123\",\"name\":\"Regression\",\"isDefault\":false}");
        using var client = TestClient.Create(handler);

        var pool = await client.Pools.CreateAsync(new CreatePoolRequest.Builder().Name("Regression").Build());

        Assert.Equal("pool-123", pool.Id);
        Assert.Equal("{\"name\":\"Regression\"}", handler.ApiRequests[0].Body);
    }

    [Fact]
    public async Task AllocationRequestOmitsUnsetFieldsAndSendsEnumAsText()
    {
        var handler = StubHandler.Json("{\"allocationId\":\"alloc-1\",\"deviceSerial\":null,\"position\":2}");
        using var client = TestClient.Create(handler);

        var allocation = await client.Allocations.CreateAsync(new DeviceAllocationRequest.Builder()
            .DevicePoolId("pool-123")
            .Priority(2)
            .RemoveApps(RemoveAppSelection.REMOVE_WITHOUT_IS_DEFAULT_APPS)
            .Build());

        var body = handler.ApiRequests[0].Body!;

        Assert.Equal("/allocation/api/v2/public/allocations", handler.ApiRequests[0].Uri.AbsolutePath);
        Assert.Contains("\"devicePoolId\":\"pool-123\"", body);
        Assert.Contains("\"priority\":2", body);
        Assert.Contains("\"removeApps\":\"REMOVE_WITHOUT_IS_DEFAULT_APPS\"", body);
        Assert.DoesNotContain("serial", body);
        Assert.DoesNotContain("manufacturer", body);
        Assert.DoesNotContain("platformVersion", body);

        Assert.Equal("alloc-1", allocation.AllocationId);
        Assert.Null(allocation.DeviceSerial);
        Assert.Equal(2, allocation.Position);
    }

    [Fact]
    public async Task StartSessionSendsVideoRecordingScopeAndReadsItBack()
    {
        var handler = StubHandler.Json(
            "{\"sessionId\":\"session-1\",\"allocationId\":\"alloc-1\"," +
            "\"videoRecording\":true,\"videoRecordingScope\":\"APPIUM_SESSION\",\"endDate\":null}");

        using var client = TestClient.Create(handler);

        var session = await client.Sessions.StartAsync(new DeviceStartSessionRequest.Builder()
            .AllocationId("alloc-1")
            .UserId(42)
            .UserEmail("qa@example.com")
            .CompanyId(7)
            .CompanyName("Example Company")
            .VideoRecording(true)
            .VideoRecordingScope(VideoRecordingScope.APPIUM_SESSION)
            .Build());

        var body = handler.ApiRequests[0].Body!;

        Assert.Equal("/session/api/v2/public/sessions", handler.ApiRequests[0].Uri.AbsolutePath);
        Assert.Contains("\"allocationId\":\"alloc-1\"", body);
        Assert.Contains("\"videoRecording\":true", body);
        Assert.Contains("\"videoRecordingScope\":\"APPIUM_SESSION\"", body);
        Assert.DoesNotContain("sessionId", body);
        Assert.DoesNotContain("appiumVersion", body);

        Assert.Equal("session-1", session.SessionId);
        Assert.Equal(VideoRecordingScope.APPIUM_SESSION, session.VideoRecordingScope);
        Assert.Null(session.EndDate);
    }

    [Fact]
    public async Task StartSessionDefaultsToFullSessionRecordingScope()
    {
        var handler = StubHandler.Json("{\"sessionId\":\"session-1\"}");
        using var client = TestClient.Create(handler);

        await client.Sessions.StartAsync(new DeviceStartSessionRequest.Builder()
            .AllocationId("alloc-1")
            .UserId(42)
            .UserEmail("qa@example.com")
            .CompanyId(7)
            .CompanyName("Example Company")
            .Build());

        Assert.Contains("\"videoRecordingScope\":\"FULL_SESSION\"", handler.ApiRequests[0].Body!);
        Assert.Contains("\"videoRecording\":false", handler.ApiRequests[0].Body!);
    }

    [Fact]
    public async Task StopSessionAndLogsUseTheSessionPath()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("appium log line"))
        });

        using var client = TestClient.Create(handler);

        await client.Sessions.StopAsync("session-1");
        var logs = await client.Sessions.LogsAsync("session-1");

        Assert.Equal(HttpMethod.Delete, handler.ApiRequests[0].Method);
        Assert.Equal("/session/api/v2/public/sessions/session-1", handler.ApiRequests[0].Uri.AbsolutePath);
        Assert.Equal(
            "/session/api/v2/public/sessions/session-1/appium-log",
            handler.ApiRequests[1].Uri.AbsolutePath);
        Assert.Equal("appium log line", Encoding.UTF8.GetString(logs));
    }

    [Fact]
    public async Task ScreenRecordsUseTheStoragePath()
    {
        var handler = StubHandler.Json(Payloads.EmptyPage);
        using var client = TestClient.Create(handler);

        await client.Sessions.ScreenRecordsAsync("session-1", new ScreenRecordPaginationRequest.Builder()
            .AddFilter(ScreenRecordFilter.CREATED_AT, "2026-01-01", SearchOperation.GREATER_THAN)
            .Build());

        var query = HttpUtility.ParseQueryString(handler.ApiRequests[0].Uri.Query);

        Assert.Equal(
            "/storage/api/v1/public/sessions/session-1/screen-records",
            handler.ApiRequests[0].Uri.AbsolutePath);
        Assert.Equal("CREATED_AT", query["filters[0].key"]);
        Assert.Equal("GREATER_THAN", query["filters[0].operation"]);
    }

    [Fact]
    public async Task UploadSendsFileHeadersAndImageInjectionPreference()
    {
        var handler = StubHandler.Json("{\"fileKey\":\"application-key\",\"size_in_bytes\":2048}");
        using var client = TestClient.Create(handler);

        using var first = new MemoryStream(Encoding.UTF8.GetBytes("default"));
        var application = await client.Applications.UploadAsync(first, "mobile-app.apk", "1.0.0");

        using var second = new MemoryStream(Encoding.UTF8.GetBytes("enabled"));
        await client.Applications.UploadAsync(second, "mobile-app.apk", "1.0.0", imageInjection: true);

        Assert.Equal("/storage/api/v1/public/applications", handler.ApiRequests[0].Uri.AbsolutePath);
        Assert.Equal("mobile-app.apk", handler.ApiRequests[0].Headers["file-name"]);
        Assert.Equal("1.0.0", handler.ApiRequests[0].Headers["version"]);
        Assert.Equal("false", handler.ApiRequests[0].Headers["image-injection"]);
        Assert.Equal("true", handler.ApiRequests[1].Headers["image-injection"]);

        Assert.Equal("application-key", application.FileKey);
        Assert.Equal(2048, application.SizeInBytes);
    }

    [Fact]
    public async Task UploadValidatesFileNameAndVersion()
    {
        var handler = StubHandler.Json("{}");
        using var client = TestClient.Create(handler);

        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<DeviceParkConfigException>(
            () => client.Applications.UploadAsync(stream, "   ", "1.0.0"));
        await Assert.ThrowsAsync<DeviceParkConfigException>(
            () => client.Applications.UploadAsync(stream, "app.apk", "   "));
        await Assert.ThrowsAsync<DeviceParkConfigException>(
            () => client.Applications.UploadAsync(stream, "app.apk", new string('v', 51)));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task DeleteApplicationAndAllocationValidateTheirIdentifiers()
    {
        var handler = StubHandler.Json("{}");
        using var client = TestClient.Create(handler);

        await Assert.ThrowsAsync<DeviceParkConfigException>(() => client.Applications.DeleteAsync(" "));
        await Assert.ThrowsAsync<DeviceParkConfigException>(() => client.Allocations.DeleteAsync(" "));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task UnknownResponseFieldsAreIgnored()
    {
        var handler = StubHandler.Json(Payloads.DevicePage);
        using var client = TestClient.Create(handler);

        var page = await client.Devices.ListAsync();

        Assert.Equal("Xiaomi", page.Data[0].Manufacturer);
    }

    [Fact]
    public async Task NullDataArrayBecomesAnEmptyPage()
    {
        var handler = StubHandler.Json(
            "{\"size\":20,\"page\":0,\"totalPages\":0,\"totalElements\":0,\"data\":null}");

        using var client = TestClient.Create(handler);

        var page = await client.Devices.ListAsync();

        Assert.True(page.IsEmpty);
        Assert.Empty(page.Data);
    }
}
