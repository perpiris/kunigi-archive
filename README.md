# Kunigi Archive

[![CI](https://github.com/perpiris/kunigi-archive/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/perpiris/kunigi-archive/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)

The archive of **"Το Κυνήγι Θησαυρού"**, the annual treasure hunt of Rethymno, Crete. For every year's game it keeps the team that organised it and the team that won, the game's days, every puzzle with its question and answer, posts, and photo, video and PDF material. The site is in Greek.

**Live:** https://tokinigi.gr

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
| `src/KunigiArchive.Web` | The site: Blazor pages, REST API, middleware |
| `ops/vps` | Server setup, secrets, backups ([runbook](ops/vps/README.md)) |

## Run locally

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Docker (for SQL Server).

```bash
docker compose -f docker-compose.dev.yml up -d                       # SQL Server on 127.0.0.1:1433
dotnet run --project src/KunigiArchive.Web --launch-profile http   # http://localhost:5256
```

On first start the app creates its database (`KunigiArchiveV2`), applies the migrations, and seeds the development accounts from `appsettings.Development.json`. In development:

- uploads go to `kunigi_media_v2/` in the repo
- emails aren't sent: they're saved as `.eml` files in `kunigi_mail_v2/` (open one to read a temporary password)
- the API is described at `/swagger`

`dotnet watch --project src/KunigiArchive.Web` reloads on changes. Stop SQL Server with `docker compose -f docker-compose.dev.yml stop`; its data stays in the `kunigi-dev_sql_data` volume.

For real content locally, restore a backup's `databases/KunigiArchiveV2.bak` into the dev SQL Server and copy its `media_v2/` folder to `kunigi_media_v2/` (see the restore steps in [`ops/vps/README.md`](ops/vps/README.md)).

## Run with Docker

The same image that runs in production, with development settings, next to SQL Server:

```bash
docker compose -f docker-compose.dev.yml --profile app up -d --build   # http://localhost:8080
```

Uploads and emails still land in `kunigi_media_v2/` and `kunigi_mail_v2/`. Stop everything with `docker compose -f docker-compose.dev.yml --profile app stop`.

To build only the image: `docker build -f src/KunigiArchive.Web/Dockerfile -t kunigi-web .`

## Build and checks

```bash
dotnet build KunigiArchive.slnx -c Release -warnaserror -warnnotaserror:NU1901,NU1902,NU1903,NU1904
```

CI runs this on every pull request (warnings fail the build, except known vulnerable-package warnings, #15) and builds the Docker image. `master` is protected: changes go in through a squash-merged PR.

Database changes are EF Core migrations in `src/KunigiArchive.Infrastructure/Persistence/Migrations`, applied automatically at startup:

```bash
dotnet ef migrations add <Name> --project src/KunigiArchive.Infrastructure --startup-project src/KunigiArchive.Web --output-dir Persistence/Migrations
```

## Deploy

Deploys are a manual GitHub Action from `master`, **Deploy web**: it copies the repo to the VPS (replacing the folder, so deleted files don't linger), rebuilds the image there with `docker compose up -d --build` and checks that https://tokinigi.gr answers. Configuration and secrets live in env files on the server, never in the repo. Server layout, secrets, backups and restores are in [`ops/vps/README.md`](ops/vps/README.md).
