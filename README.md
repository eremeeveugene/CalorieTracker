# Calorie Tracker

[![CI](https://github.com/eremeeveugene/CalorieTracker/actions/workflows/ci.yml/badge.svg)](https://github.com/eremeeveugene/CalorieTracker/actions/workflows/ci.yml)

A cross-platform calorie tracker with AI-assisted food logging.

- **Client:** Avalonia UI (MVVM) for Windows, macOS, Linux, Android, iOS and the browser
- **Backend:** ASP.NET Core microservices behind a YARP gateway, orchestrated locally with .NET Aspire
- **Data:** PostgreSQL (one database per service), Redis, RabbitMQ
- **Design:** Clean Architecture per service, GoF patterns where they solve a real problem

## Getting started

Requirements: .NET SDK 10.0.403+ (see `global.json`) and Docker Desktop.

```bash
dotnet tool restore
dotnet build CalorieTracker.slnx
dotnet test CalorieTracker.CI.slnf
dotnet run --project src/AppHost
```

## Repository layout

| Path | Contents |
|---|---|
| `src/AppHost` | .NET Aspire orchestration |
| `src/Gateway` | YARP API gateway |
| `src/Services/*` | Microservices (Domain / Application / Infrastructure / Api) |
| `src/BuildingBlocks` | Shared kernel: Result, Mediator, event bus abstractions |
| `src/Contracts` | Integration events and DTOs shared by services and the client |
| `src/Client` | Avalonia client: shared code plus Desktop, Android, iOS and Browser heads |
| `tests` | Unit, integration and architecture tests |
| `docs` | Architecture decision records and the security checklist |

## Conventions

- **Branches:** GitHub Flow. `main` is always deployable; work happens in short-lived branches merged by PR.
- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `refactor:`, `feat!:` for breaking changes).
- **Versions:** SemVer calculated from git tags (`v1.2.3`) by MinVer. Never edit a version number by hand.
- **Packages:** all versions live in `Directory.Packages.props`.
- **Code quality:** analyzer warnings fail the build.

## Security

See [docs/SECURITY.md](docs/SECURITY.md) for how to report a vulnerability and for the security checklist.
