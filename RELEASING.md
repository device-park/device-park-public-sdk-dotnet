# Releasing

This repository produces the official `Testinium.DevicePark` package for nuget.org.

## Package Identity

| Field | Value |
|---|---|
| NuGet package | `Testinium.DevicePark` |
| Assembly / root namespace | `Testinium.DevicePark` |
| Feed | `https://api.nuget.org/v3/index.json` |
| Target frameworks | `netstandard2.0`, `net472`, `net8.0` |
| Runtime dependencies | none |
| Java artifact | `io.testinium.devicepark:device-park-public-sdk` |
| npm package | `@device-park/public-sdk` |

## Release Requirements

- Build with the **.NET 10 SDK** (current LTS). It compiles all three target frameworks; the
  .NET 9 SDK also works but is out of support as of May 2026.
- Update `<Version>` in `src/Testinium.DevicePark/Testinium.DevicePark.csproj`.
- Never commit NuGet API keys. Supply `NUGET_API_KEY` through the CI secret store.
- Publish only from a release tag.
- Keep `netstandard2.0` in the target framework list. Dropping it removes .NET Framework,
  .NET Core 3.1 and Unity/Xamarin support.
- Do not add a `PackageReference` without reconsidering the zero-dependency guarantee. On
  .NET Framework, `System.Text.Json` alone pulls in nine transitive packages.

## Pipeline Commands

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release -o artifacts
dotnet nuget push "artifacts/*.nupkg" \
  --source https://api.nuget.org/v3/index.json \
  --api-key "$NUGET_API_KEY" \
  --skip-duplicate
```

`IncludeSymbols` is enabled, so `dotnet pack` also produces a `.snupkg` symbol package. Push it
alongside the main package.

## Post-Release Verification

```bash
dotnet new console -o /tmp/dp-check && cd /tmp/dp-check
dotnet add package Testinium.DevicePark
```

Then confirm that a clean consumer project compiles against `DeviceParkApiClient` and that the
restore graph contains no package other than the SDK itself:

```bash
grep -c '"type": "package"' obj/project.assets.json
```

## Runtime Compatibility Checks

`device-park-compat/` contains one single-target runner project per runtime. Re-run it before a
release that changes target frameworks or the JSON layer. See that folder's `README.md` for the
exact commands and for the MSBuild pitfall that invalidates the measurement if ignored.
