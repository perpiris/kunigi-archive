# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build
dotnet run --project KunigiArchive.Web
dotnet watch run --project KunigiArchive.Web   # with hot reload
```

Launch profiles (from `launchSettings.json`):
- `http`: `http://localhost:5000`
- `https`: `https://localhost:5001`

## Database Migrations

Migrations apply automatically on startup. To manage manually:

```bash
dotnet ef migrations add <Name> --project KunigiArchive.Web --output-dir Data/Migrations
dotnet ef database update --project KunigiArchive.Web
```

The SQLite database file is `kunigi_archive.db` (local, not committed).

## Architecture

**Stack:** .NET 10, ASP.NET Core MVC, Entity Framework Core (SQLite), ASP.NET Core Identity, Serilog

**Pattern:** Service-oriented MVC. Controllers are thin — all business logic lives in scoped services (`AccountService`, `TeamService`, `GameService`, `DirectoryService`, `EmailService`). Services depend directly on `DataContext`.

**Auth:** Role-based via ASP.NET Core Identity. Two roles: `Admin` and `Manager`. Managers are linked to specific teams via the `TeamManager` join entity, not just a role assignment.

**Routing:** Entities expose a `Slug` property used for URL segments (e.g., `/teams/team-slug`, `/games/game-slug`). The `SlugGenerator` utility in `Utilities/` handles slug creation.

**Media:** File uploads are stored under `wwwroot/media/`. `DirectoryService` manages path resolution. Media entities (`TeamMedia`, `GameMedia`, `GameDayMedia`, `PuzzleMedia`) store file references separately from their parent entities.

**Domain model core relationships:**
- A `Game` has one host `Team` and optionally one winner `Team`
- A `Game` contains `GameDay` entries, each with a `GameDayType`
- A `GameDay` contains `Puzzle` entries
- Teams have `TeamManager` entries linking `AppUser` records to specific teams

**Configuration:** `appsettings.json` holds production values; `appsettings.Development.json` overrides for local dev (debug logging, localhost URLs). `EmailSettings` is bound from config to a typed `Settings/EmailSettings.cs` class.

**Seeding:** `Data/DataSeed.cs` seeds initial data and runs on every startup (idempotent).
