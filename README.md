# Kunigi Archive

[![CI](https://github.com/perpiris/kunigi-archive/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/perpiris/kunigi-archive/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)

[![Deploy web](https://github.com/perpiris/kunigi-archive/actions/workflows/deploy-web.yml/badge.svg)](https://github.com/perpiris/kunigi-archive/actions/workflows/deploy-web.yml)
[![Web deployed](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Fperpiris%2Fkunigi-archive%2Factions%2Fworkflows%2Fdeploy-web.yml%2Fruns%3Fper_page%3D1%26status%3Dsuccess&query=%24.workflow_runs%5B0%5D.updated_at&label=tokinigi.gr%20deployed&color=ff4d00)](https://tokinigi.gr)

[![Deploy beta](https://github.com/perpiris/kunigi-archive/actions/workflows/deploy-beta.yml/badge.svg)](https://github.com/perpiris/kunigi-archive/actions/workflows/deploy-beta.yml)
[![Beta deployed](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Fperpiris%2Fkunigi-archive%2Factions%2Fworkflows%2Fdeploy-beta.yml%2Fruns%3Fper_page%3D1%26status%3Dsuccess&query=%24.workflow_runs%5B0%5D.updated_at&label=beta.tokinigi.gr%20deployed&color=2e5cff)](https://beta.tokinigi.gr)

[![Schema changed](https://img.shields.io/github/last-commit/perpiris/kunigi-archive/master?path=src%2FKunigiArchive.Infrastructure%2FPersistence%2FMigrations&label=schema%20changed&color=ffd600)](https://github.com/perpiris/kunigi-archive/commits/master/src/KunigiArchive.Infrastructure/Persistence/Migrations)

The archive of **"Το Κυνήγι Θησαυρού"**, the annual treasure hunt of Rethymno, Crete. For every year's game it keeps the team that organised it and the team that won, the game's days, every puzzle with its question and answer, posts, and photo, video and PDF material. The site is in Greek.

- **Live:** https://tokinigi.gr
- **Beta:** https://beta.tokinigi.gr (test data, not indexed)

Visitors browse teams, games, days and puzzles. Team managers edit their own teams and the games those teams organised; admins manage everything, including user accounts.

## Tech

- **.NET 10 (LTS)**: Blazor Web App (static server rendering with enhanced navigation) plus a small REST API under `/api`
- **SQL Server** through EF Core; ASP.NET Core Identity for accounts (roles `Admin` and `Manager`)
- **ImageSharp 3.1** (shrinks uploads to WebP and makes thumbnails), **PDFtoImage** (first PDF page to an image)
- **Serilog** logging, **Quill** rich-text editor (sanitized on save and on display), **GLightbox** gallery
- Docker images behind **nginx-proxy** with Let's Encrypt on a single VPS; GitHub Actions for CI and manual deploys

## Project layout

| Path | What it is |
|---|---|
| `src/KunigiArchive.Domain` | Entities and rules (teams, games, days, puzzles, slugs) |
| `src/KunigiArchive.Application` | Use cases (one handler per action), queries, authorization |
| `src/KunigiArchive.Infrastructure` | EF Core, migrations, Identity, media storage, email |
| `src/KunigiArchive.Web2` | The site: Blazor pages, REST API, middleware |
| `src/KunigiArchive.Import` | Re-runnable import from the old site's database and media |
| `src/KunigiArchive.Web` | The old ASP.NET Core MVC site, being retired |
| `ops/vps` | Server setup, secrets, backups ([runbook](ops/vps/README.md)) |

## Run locally

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Docker (for SQL Server).

```bash
docker compose -f docker-compose.dev.yml up -d                       # SQL Server on 127.0.0.1:1433
dotnet run --project src/KunigiArchive.Web2 --launch-profile http   # http://localhost:5256
```

On first start the app creates its database (`KunigiArchiveV2`), applies the migrations, and seeds the development accounts from `appsettings.Development.json`. In development:

- uploads go to `kunigi_media_v2/` in the repo
- emails aren't sent: they're saved as `.eml` files in `kunigi_mail_v2/` (open one to read a temporary password)
- the API is described at `/swagger`

`dotnet watch --project src/KunigiArchive.Web2` reloads on changes. Stop SQL Server with `docker compose -f docker-compose.dev.yml stop`; its data stays in the `kunigi-dev_sql_data` volume.

For real content, `src/KunigiArchive.Import` copies an old-site database and media folder into the new one. It's a dry run unless you add `--apply`; the usage is at the top of its `Program.cs`.

## Run with Docker

The same image that runs in production, with development settings, next to SQL Server:

```bash
docker compose -f docker-compose.dev.yml --profile app up -d --build   # http://localhost:8080
```

Uploads and emails still land in `kunigi_media_v2/` and `kunigi_mail_v2/`. Stop everything with `docker compose -f docker-compose.dev.yml --profile app stop`.

To build only the image: `docker build -f src/KunigiArchive.Web2/Dockerfile -t kunigi-web2 .`

## Build and checks

```bash
dotnet build KunigiArchive.slnx -c Release -warnaserror -warnnotaserror:NU1901,NU1902,NU1903,NU1904
```

CI runs this on every pull request (warnings fail the build, except known vulnerable-package warnings, #15) and builds both Docker images. `master` is protected: changes go in through a squash-merged PR.

Database changes are EF Core migrations in `src/KunigiArchive.Infrastructure/Persistence/Migrations`, applied automatically at startup:

```bash
dotnet ef migrations add <Name> --project src/KunigiArchive.Infrastructure --startup-project src/KunigiArchive.Web2 --output-dir Persistence/Migrations
```

## Deploy

Deploys are manual GitHub Actions from `master`: **Deploy web** (tokinigi.gr) and **Deploy beta** (beta.tokinigi.gr). Each copies the repo to the VPS, rebuilds the image there and checks that the site answers. Configuration and secrets live in env files on the server, never in the repo. Server layout, secrets, backups and restores are in [`ops/vps/README.md`](ops/vps/README.md).
