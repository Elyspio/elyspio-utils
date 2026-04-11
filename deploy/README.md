# deploy

This folder contains local PowerShell helpers for releasing the packages in this repository.

## Scripts

| Script | What it does |
| --- | --- |
| `elyspio-vite-eslint-config.publish.ps1` | Changes into `..\vite-eslint-config` and runs `npm run publish`. |
| `elyspio-utils-telemetry.publish.ps1` | Calls `scripts\publish-dotnet.ps1` for `..\elyspio-utils-telemetry`. |
| `scripts\publish-dotnet.ps1` | Cleans the solution, packs the projects, then pushes every generated `.nupkg` to NuGet.org. |

## Requirements

- PowerShell
- Node.js and `pnpm` for `vite-eslint-config`
- .NET SDK for `elyspio-utils-telemetry`
- `deploy\configs\nuget.token` containing the NuGet API key used by `publish-dotnet.ps1`

Any token files under `deploy\configs` are local-only and ignored by git.

## Usage

Run the publish helpers from the repository root:

```powershell
.\deploy\elyspio-vite-eslint-config.publish.ps1
.\deploy\elyspio-utils-telemetry.publish.ps1
```

For package-specific details, use the package docs:

- [../vite-eslint-config/README.md](../vite-eslint-config/README.md)
- [../elyspio-utils-telemetry/readme.md](../elyspio-utils-telemetry/readme.md)
