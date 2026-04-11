# elyspio-vite-eslint-config

This repository groups Elyspio shared packages and the local scripts used to publish them.

## Repository layout

| Path | Purpose | Documentation |
| --- | --- | --- |
| `vite-eslint-config` | Shared Vite+ defaults for React, TypeScript, Oxlint and Oxfmt projects. | [vite-eslint-config/README.md](./vite-eslint-config/README.md) |
| `elyspio-utils-telemetry` | .NET 10 OpenTelemetry bootstrap packages for ASP.NET Core, plus an example app. | [elyspio-utils-telemetry/readme.md](./elyspio-utils-telemetry/readme.md) |
| `deploy` | PowerShell helpers for publishing the packages in this repository. | [deploy/README.md](./deploy/README.md) |

## Working in this repo

There is no single top-level build for the whole repository. Work from the package you want to change and use that package's own commands and documentation:

- `vite-eslint-config` for the JavaScript package.
- `elyspio-utils-telemetry` for the .NET packages.
- `deploy` for local publishing helpers.

## Publishing

Publishing is handled from the `deploy` folder:

- `deploy\elyspio-vite-eslint-config.publish.ps1` publishes the npm package from `vite-eslint-config`.
- `deploy\elyspio-utils-telemetry.publish.ps1` packs and pushes the .NET packages from `elyspio-utils-telemetry`.

See [deploy/README.md](./deploy/README.md) for the expected local setup.
