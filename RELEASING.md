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

## Versioning

**The release tag is the only source of truth for the published version.**
`.github/workflows/publish.yaml` reads the tag, strips an optional leading `v`, and passes the
result to build, test and pack as `-p:Version=`.

`<Version>` in `src/Testinium.DevicePark/Testinium.DevicePark.csproj` is deliberately pinned to
`0.0.0-dev`. It applies to local builds only and is overridden on every release. Bumping it
publishes nothing and releases nothing — leave it alone.

Versions follow [SemVer](https://semver.org/). Pick the next one from what landed since the
previous tag (`git log <last-tag>..HEAD --oneline`):

| Change | Bump | Example |
|---|---|---|
| Breaking public API change, or a target framework removed | MAJOR | `1.4.2` → `2.0.0` |
| New public API, backward compatible | MINOR | `1.4.2` → `1.5.0` |
| Bug fix only, public API unchanged | PATCH | `1.4.2` → `1.4.3` |

Use a pre-release tag (`1.5.0-rc.1`, `1.5.0-preview.2`) when the change needs validation by a
real consumer first. nuget.org hides pre-releases from default search and restore, so they are
safe to publish ahead of the final version.

Existing tags use the bare form (`1.0.0`, not `v1.0.0`). Keep it consistent.

## Cutting a Release

Publishing is triggered by a **published GitHub release**, not by pushing a tag and not by
running `dotnet nuget push` by hand:

```bash
git log 1.0.0..HEAD --oneline          # what is going out?
gh release create 1.1.0 --target dev --title "1.1.0" --generate-notes
```

The workflow then restores, builds, tests, packs, verifies the package layout, authenticates to
nuget.org and pushes. `IncludeSymbols` is enabled, so `dotnet pack` also produces a `.snupkg`
symbol package, pushed alongside the main package by the same glob.

A published version on nuget.org is immutable — it can be unlisted but never replaced. If a
release is wrong, unlist it and ship a new patch version.

## Release Requirements

- The workflow pins the **.NET 10 SDK** (current LTS); it compiles all three target frameworks.
  Match it locally when reproducing a release build.
- Never commit NuGet credentials. The workflow authenticates through `NuGet/login@v1` with the
  `NUGET_USER` secret and the job's `id-token: write` permission, so no long-lived API key is
  stored in the repository.
- Publish only from a GitHub release. Do not push packages from a developer machine.
- Keep `netstandard2.0` in the target framework list. Dropping it removes .NET Framework,
  .NET Core 3.1 and Unity/Xamarin support — and it is a MAJOR bump.
- Do not add a `PackageReference` without reconsidering the zero-dependency guarantee. On
  .NET Framework, `System.Text.Json` alone pulls in nine transitive packages.

## Reproducing a Release Build Locally

Pass the version explicitly, the same way the pipeline does:

```bash
dotnet restore
dotnet build -c Release -p:Version=1.1.0
dotnet test  -c Release -p:Version=1.1.0
dotnet pack  -c Release -o artifacts -p:Version=1.1.0
```

Without `-p:Version`, the package is produced as `0.0.0-dev` from the csproj default.

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
