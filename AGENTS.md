# Repository Guidelines

## Project Structure & Module Organization

This repository contains independently built shared packages; there is no root-level build.

- `elyspio-utils-telemetry/` contains .NET 10 OpenTelemetry libraries. Reusable modules are under `Packages/` (`Core`, `MongoDB`, `Redis`, and `Sql`); tests live beside the module in `Packages/Sql.Tests/`.
- `elyspio-utils-telemetry/Examples/WebApi/` is the ASP.NET Core reference application. `Examples/Databases/docker-compose.yml` provides local SQL Server, MongoDB, and Redis.
- `vite-eslint-config/` is the TypeScript/Vite+ configuration package. Source is in `src/`, with release preparation scripts in `scripts/`.
- `deploy/` contains PowerShell packaging and publishing helpers. Read its README before publishing.

## Build, Test, and Development Commands

Run commands from the relevant package directory:

```bash
dotnet build Elyspio.Utils.Telemetry.slnx
dotnet test Elyspio.Utils.Telemetry.slnx
dotnet run --project Examples/WebApi/Elyspio.Utils.Telemetry.Examples.WebApi.csproj
docker compose -f Examples/Databases/docker-compose.yml up -d
```

Use `pnpm install` in `vite-eslint-config/`, then `pnpm check` for linting/format checks and `pnpm build` to create the distributable package. Use the lockfile’s pinned pnpm version (`10.32.1`).

## Coding Style & Naming Conventions

Follow the surrounding style in each module. C# uses nullable reference types, implicit usings, tabs for indentation, PascalCase for public types and members, and `I`-prefixed interfaces (for example, `ITracingService`). Keep one concern per file and place module-specific extensions under `Extensions/`.

TypeScript uses tabs, ESM imports, and camelCase for functions and variables. Run `pnpm check` before submitting TypeScript changes; do not hand-edit generated `lib/` or `dist/` outputs.

## Testing Guidelines

Tests use xUnit and live in `*.Tests` projects. Name test classes after the subject and test methods with behavior-focused names, such as `Extracts_commands_and_tables`. Add regression coverage with every behavior change. Run targeted tests with:

```bash
dotnet test Packages/Sql.Tests/Elyspio.Utils.Telemetry.Sql.Tests.csproj
```

## Commit & Pull Request Guidelines

Recent history uses concise, package-scoped subjects, often with release markers: `Elyspio.Utils.Telemetry -- 1.3.0 -- Update dependencies` or `Vite-eslint-config -- Fix: ...`. Keep commits focused and describe the affected package and purpose. PRs should explain behavior changes, list validation performed, link relevant issues, and include screenshots only for user-visible UI or documentation rendering changes.

## Configuration & Security

Do not commit credentials or environment-specific endpoints. Treat `appsettings.json` and compose defaults as local-development examples; override secrets through user secrets or environment variables.
