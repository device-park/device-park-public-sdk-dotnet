# Device Park Public SDK for .NET

Official .NET client library for the Device Park public APIs.

The SDK provides typed clients for discovering devices, selecting device pools, reserving devices, managing test sessions and storing mobile application artifacts. Its public domain model and lifecycle follow the Device Park Java SDK so teams can use the same concepts across Java, Node.js and .NET projects.

**Zero package dependencies.** The SDK uses only types from the base class library, so it adds exactly one assembly to your project and brings no transitive NuGet packages — including on .NET Framework.

## Capabilities

- List devices, retrieve device details and inspect installed applications.
- List, create and delete device pools; resolve the default pool and manage device membership.
- Reserve a device by serial, pool or matching criteria.
- Optionally remove non-default applications before device allocation.
- Inspect active and queued allocations.
- Start multiple sequential sessions from the same active allocation.
- Stop sessions and collect Appium logs or screen recordings.
- Upload, list and delete APK or IPA artifacts.
- Enable image injection for supported application-upload workflows.
- Authenticate with OAuth2 client credentials and renew access tokens automatically.

## Supported Runtimes

Every row below was executed, not inferred. The resolved package asset was verified from each
assembly's `TargetFrameworkAttribute`.

| Runtime | Asset used | Status |
|---|---|---|
| .NET Framework 4.7.2+ | `net472` | verified (Mono 6.14.1) |
| .NET Core 3.1 | `netstandard2.0` | verified |
| .NET 5 / 6 / 7 | `netstandard2.0` | verified (5.0.17, 6.0.36) |
| .NET 8 | `net8.0` | verified (8.0.16) |
| .NET 9 | `net8.0` | verified (9.0.5) |
| .NET 10 and later | `net8.0` | verified (10.0.12) |
| Mono, Xamarin, Unity 2018.1+ | `netstandard2.0` | compatible profile |

.NET Framework 4.6.1 satisfies `netstandard2.0` but defaults to SSL3/TLS 1.0, which breaks HTTPS
calls unless the host application changes `ServicePointManager.SecurityProtocol`. Use 4.7.2 or
later, where the OS default (TLS 1.2) applies.

## Requirements

- One of the runtimes above
- Device Park `clientId` and `clientSecret`
- Network access to the Device Park environment, devices and pools

## Installation

```bash
dotnet add package Testinium.DevicePark
```

## Authentication

Store credentials in your CI secret store or environment. Do not commit them to source control.

```bash
export DEVICEPARK_CLIENT_ID=your-client-id
export DEVICEPARK_CLIENT_SECRET=your-client-secret
```

Create one client for the test run and reuse it:

```csharp
using Testinium.DevicePark;

using var client = DeviceParkApiClient.Builder()
    .Url("https://devicepark.testinium.io")
    .Credentials(Credentials.FromEnvironment())
    .Timeout(60)
    .Build();
```

`Credentials.FromEnvironment()` reads `DEVICEPARK_CLIENT_ID` and `DEVICEPARK_CLIENT_SECRET`.
Credentials already held by the application can be supplied with `Credentials.Of(clientId, clientSecret)`.

The client obtains an access token on the first SDK operation, keeps it in memory and renews it
before expiration. It also refreshes the token and retries once if a request is rejected with
HTTP 401. Create one client per test run so the cached token can be reused.

## First Device Session

A Device Park test normally follows this lifecycle:

1. List devices or pools.
2. Create an allocation to reserve a device.
3. Start a session with the returned `AllocationId`.
4. Run the Appium test.
5. Stop the session.
6. Release the allocation after the final session.
7. Dispose the SDK client.

```csharp
using Testinium.DevicePark;
using Testinium.DevicePark.Models;

using var client = DeviceParkApiClient.Builder()
    .Url("https://devicepark.testinium.io")
    .Credentials(Credentials.FromEnvironment())
    .Build();

string? allocationId = null;
string? sessionId = null;

try
{
    var devices = await client.Devices.ListAsync(new ListDevicesRequest.Builder()
        .Page(0)
        .Size(20)
        .AddFilter(DeviceFilter.STATE, "HEALTHY", SearchOperation.EQUAL)
        .Build());

    var serial = devices.Data.FirstOrDefault()?.Serial
        ?? throw new InvalidOperationException("No Device Park device is available");

    var allocation = await client.Allocations.CreateAsync(new DeviceAllocationRequest.Builder()
        .Serial(serial)
        .Priority(3)
        .Build());

    allocationId = allocation.AllocationId;

    if (allocation.DeviceSerial is null)
    {
        throw new InvalidOperationException($"Allocation {allocationId} is waiting for a device");
    }

    var session = await client.Sessions.StartAsync(new DeviceStartSessionRequest.Builder()
        .AllocationId(allocationId!)
        .UserId(42)
        .UserEmail("qa@example.com")
        .CompanyId(7)
        .CompanyName("Example Company")
        .VideoRecording(true)
        .Build());

    sessionId = session.SessionId;
    Console.WriteLine($"Session started: {sessionId}");

    // Run Appium automation for the active session.
}
finally
{
    if (sessionId is not null)
    {
        await client.Sessions.StopAsync(sessionId);
    }

    if (allocationId is not null)
    {
        await client.Allocations.DeleteAsync(allocationId);
    }
}
```

## Allocation and Session Lifecycle

An allocation is a temporary device reservation. Store its `AllocationId` immediately after creation.

If `DeviceSerial` is `null`, the allocation can be waiting in the queue. Inspect the same allocation
until a device is assigned or the test-runner deadline is reached. Do not create duplicate
allocations while waiting.

One active allocation can be reused for multiple sessions. Stop the current session before starting
the next unless concurrent sessions are enabled for the environment. Release the allocation only
after the final session.

> **A device must belong to a pool before it can be allocated.** A device that is visible in
> `Devices.ListAsync` and reports a healthy state is not necessarily allocatable; if it is not a
> member of any pool, allocation fails with `ALLOCATION_ERR003` ("No matching devices found").
> Add the device to a pool first, or ask a Device Park administrator to do so.

## SDK Services

| Service | Purpose | Main methods |
|---|---|---|
| `client.Devices` | Discover devices and installed applications | `ListAsync`, `GetAsync`, `AppsAsync`, `ListByDefaultPoolAsync` |
| `client.Pools` | Discover and manage device pools | `ListAsync`, `GetDefaultPoolAsync`, `ListByDefaultPoolAsync`, `CreateAsync`, `DeleteAsync`, `AddDevicesAsync`, `RemoveDevicesAsync` |
| `client.Allocations` | Reserve and release devices | `CreateAsync`, `ListAsync`, `DeleteAsync` |
| `client.Sessions` | Manage test sessions and artifacts | `StartAsync`, `ListAsync`, `StopAsync`, `LogsAsync`, `ScreenRecordsAsync` |
| `client.Applications` | Manage APK and IPA artifacts | `UploadAsync`, `ListAsync`, `DeleteAsync` |

All service methods are asynchronous and accept an optional `CancellationToken`.

List methods return `PageDto<T>` with `Page`, `Size`, `TotalPages`, `TotalElements`, `Data`, plus
`IsLast` and `IsEmpty` helpers.

### Pagination and filtering

Defaults are `Page = 0`, `Size = 20`, `SortBy = "ID"`, `Direction = SortDirection.DESC`.

```csharp
var request = new ListDevicesRequest.Builder()
    .Page(0)
    .Size(50)
    .SortBy("ID")
    .Direction(SortDirection.DESC)
    .AddFilter(DeviceFilter.PLATFORM, "android", SearchOperation.EQUAL)
    .Build();
```

Filter keys are sent as their constant names. `DeviceFilter.STATE` accepts Device Park state values
such as `HEALTHY`; note that the `Device.State` property returns the display name (`Online`), which
is not the same string you filter on.

### Device pools

```csharp
var defaultPool = await client.Pools.GetDefaultPoolAsync();

var pool = await client.Pools.CreateAsync(
    new CreatePoolRequest.Builder().Name("Android Regression").Build());

await client.Pools.AddDevicesAsync(pool.Id!, new[] { "SERIAL-1", "SERIAL-2" });
await client.Pools.RemoveDevicesAsync(pool.Id!, new[] { "SERIAL-1" });
await client.Pools.DeleteAsync(pool.Id!);
```

### Installed applications on a device

```csharp
var apps = await client.Devices.AppsAsync(serial, new ListDeviceAppsRequest.Builder()
    .AddFilter(DeviceAppFilter.IS_DEFAULT, true, SearchOperation.EQUAL)
    .Build());
```

### Clean device before allocation

Allocation requests keep installed applications by default.

```csharp
var request = new DeviceAllocationRequest.Builder()
    .DevicePoolId("android-regression")
    .RemoveApps(RemoveAppSelection.REMOVE_WITHOUT_IS_DEFAULT_APPS)
    .Build();
```

### Upload an application

```csharp
using var apk = File.OpenRead("/path/to/mobile-app.apk");

var application = await client.Applications.UploadAsync(apk, "mobile-app.apk", "1.0.0");
Console.WriteLine(application.FileKey);
```

Image injection is disabled by default. Enable it only when the workflow requires Device Park
gadget injection:

```csharp
var application = await client.Applications.UploadAsync(
    apk, "mobile-app.apk", "1.0.0", imageInjection: true);
```

Use `FileKey` as the stable application identifier. A returned `DownloadUrl` can be temporary or null.

## Timestamps

All timestamp properties are exposed as `string?` exactly as the API returns them. Device Park
returns microsecond precision and mixed time-zone suffixes (`2026-10-02T06:50:09.765655179Z`
alongside `2026-10-01T12:05:28.207100873`), so the SDK does not parse them for you. Parse with
`DateTimeOffset.Parse` in your own code when you need a date type.

## Error Handling

```csharp
using Testinium.DevicePark.Errors;

try
{
    var devices = await client.Devices.ListAsync();
    Console.WriteLine(devices.Data.Count);
}
catch (DeviceParkConfigException error)
{
    Console.Error.WriteLine($"Invalid SDK configuration: {error.Message}");
}
catch (DeviceParkHttpException error)
{
    Console.Error.WriteLine($"Device Park request failed: {error.Status}");
    Console.Error.WriteLine($"{error.ErrorCode}: {error.ErrorMessage}");
    Console.Error.WriteLine(error.Body);
}
catch (DeviceParkSerializationException error)
{
    Console.Error.WriteLine($"Unexpected response format: {error.Message}");
}
catch (TaskCanceledException)
{
    Console.Error.WriteLine("Device Park request timed out");
}
```

| Exception | When it is used | Useful members |
|---|---|---|
| `DeviceParkConfigException` | Missing client configuration or invalid SDK argument | `Message` |
| `DeviceParkHttpException` | Device Park returned a non-2xx response | `Status`, `ErrorCode`, `ErrorMessage`, `Body` |
| `DeviceParkSerializationException` | JSON parsing or serialization failed | `Message`, `InnerException` |
| `DeviceParkException` | Base class for SDK-defined errors | `Message` |

`ErrorCode` and `ErrorMessage` are populated when the API returns its standard error envelope
(`{"errorCode","errorMessage","errorDetails"}`); both are `null` otherwise. `Body` always carries
the raw response.

A request timeout surfaces as `TaskCanceledException`, not as `DeviceParkHttpException`.

Never log client secrets, access tokens or authorization headers.

## Cleanup

Production test runners should clean up from a `finally` block:

1. Stop active sessions.
2. Release allocations after their final session.
3. Dispose the SDK client.

Handle cleanup failures separately so they do not replace the original test failure.

## Dependency Injection

The SDK has no dependency on any host or DI container. Register it yourself if you use one:

```csharp
services.AddSingleton(_ => DeviceParkApiClient.Builder()
    .Url(configuration["DevicePark:Url"]!)
    .Credentials(Credentials.Of(
        configuration["DevicePark:ClientId"]!,
        configuration["DevicePark:ClientSecret"]!))
    .Build());
```

To route SDK traffic through your own handler pipeline — for logging, proxies or tests — pass a
handler:

```csharp
.HttpMessageHandler(new MyLoggingHandler(new HttpClientHandler()))
```

## Documentation and Support

- [Device Park SDK documentation](https://github.com/device-park/device-park-api-docs)
- [Source repository](https://github.com/device-park/device-park-public-sdk-dotnet)
- [Issue tracker](https://github.com/device-park/device-park-public-sdk-dotnet/issues)

When reporting a problem, include the SDK version, target framework and relevant resource
identifiers. Remove credentials, tokens and sensitive application data from logs.

## License

Licensed under the [MIT License](LICENSE).
