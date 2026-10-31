# PulseAlert

PulseAlert is a server and dashboard for tracking GPS devices and responding to SOS signals, inspired by Garmin inReach.

[docs/PLAN.md](docs/PLAN.md) is the design document and the source of truth for the webhook contract, endpoints, business rules and roadmap. Read it before starting any feature work, and follow it rather than inventing behavior.

## Stack

- .NET 10 ASP.NET Core API with SignalR
- PostgreSQL with EF Core 10 (Npgsql provider)
- React and TypeScript with React Query, served by the API
- Docker, GitHub Actions, and Terraform on Azure (Container Apps and Azure Database for PostgreSQL Flexible Server)
- xUnit and Moq, with Testcontainers for integration tests

## Layout

- `src/PulseAlert.Core`: entities and domain types. No infrastructure dependencies.
- `src/PulseAlert.Infrastructure`: `PulseAlertDbContext`, entity configurations, migrations, and `AddInfrastructure()`.
- `src/PulseAlert.Api`: controllers, filters, request contracts, and `Program.cs`.
- `tests/PulseAlert.UnitTests`: xUnit tests.
- `PulseAlert.slnx` is the solution file. `global.json` pins the SDK. `Directory.Build.props` turns on nullable types, implicit usings, and warnings as errors.

## Commands

```bash
docker compose up -d
dotnet tool restore
dotnet ef database update --project src/PulseAlert.Infrastructure --startup-project src/PulseAlert.Api
dotnet run --project src/PulseAlert.Api
```

The API runs on http://localhost:5229.

Before opening a PR, run what CI runs:

```bash
dotnet format PulseAlert.slnx --verify-no-changes
dotnet build PulseAlert.slnx -c Release
dotnet test PulseAlert.slnx -c Release
```

Add a migration with `dotnet ef migrations add <Name> --project src/PulseAlert.Infrastructure --startup-project src/PulseAlert.Api --output-dir Data/Migrations`.

## Workflow

- Make one small branch per piece of work (`feature/...` or `chore/...`), created from an up-to-date `main`.
- Write the code, then test it locally: format check, Release build, tests, and a live check against the local Postgres where useful.
- Commit, push, and open a PR with `gh`. The owner reviews it on GitHub.
- Merge only after the owner says so, always with a merge commit (`gh pr merge <n> --merge --delete-branch`), never squash. Then run `git fetch --prune`.
- Before starting a new roadmap phase, stop and check with the owner.
- Several sessions may share this folder. Check `git status` and `gh pr list` before switching branches, and use a separate worktree if another branch is in progress.

## Conventions

- Devices are identified externally by IMEI. The internal Guid `DeviceId` is not exposed in the API.
- Store enum statuses as strings, and use `DeleteBehavior.Restrict` on foreign keys.
- Write PLAN.md edits as plain statements of the decision, with no notes about what changed or what was wrong before. The owner updates the diagram images in `docs/images` themselves.

## Gotchas

- Tests that read model details such as `IsDescending` need `context.GetService<IDesignTimeModel>().Model`. The runtime model drops that information.
- Don't use `dotnet ef database update --no-build`. It can apply a stale build and silently do nothing.
- `.gitattributes` forces LF line endings. Don't commit CRLF files.
- Don't run destructive git commands such as `reset --hard` or force pushes.
- The C: drive has run low on space before. If a build fails with a disk error, check free space first.
