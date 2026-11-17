# Kunigi Archive

[![CI](https://github.com/perpiris/kunigi-archive/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/perpiris/kunigi-archive/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)

[![Deploy web](https://github.com/perpiris/kunigi-archive/actions/workflows/deploy-web.yml/badge.svg)](https://github.com/perpiris/kunigi-archive/actions/workflows/deploy-web.yml)
[![Web deployed](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Fperpiris%2Fkunigi-archive%2Factions%2Fworkflows%2Fdeploy-web.yml%2Fruns%3Fper_page%3D1%26status%3Dsuccess&query=%24.workflow_runs%5B0%5D.updated_at&label=tokinigi.gr%20deployed&color=ff4d00)](https://tokinigi.gr)

[![Deploy beta](https://github.com/perpiris/kunigi-archive/actions/workflows/deploy-beta.yml/badge.svg)](https://github.com/perpiris/kunigi-archive/actions/workflows/deploy-beta.yml)
[![Beta deployed](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Fperpiris%2Fkunigi-archive%2Factions%2Fworkflows%2Fdeploy-beta.yml%2Fruns%3Fper_page%3D1%26status%3Dsuccess&query=%24.workflow_runs%5B0%5D.updated_at&label=beta.tokinigi.gr%20deployed&color=2e5cff)](https://beta.tokinigi.gr)

[![Schema changed](https://img.shields.io/github/last-commit/perpiris/kunigi-archive/master?path=src%2FKunigiArchive.Web%2FData%2FMigrations&label=schema%20changed&color=ffd600)](https://github.com/perpiris/kunigi-archive/commits/master/src/KunigiArchive.Web/Data/Migrations)

Archive site for "Το Κυνήγι Θησαυρού", the annual scavenger hunt in Rethymno, Crete: the teams that hosted and won each game, its days, puzzles, posts and media.

- **Live:** https://tokinigi.gr (`src/KunigiArchive.Web`, ASP.NET Core MVC)
- **Beta:** https://beta.tokinigi.gr (`src/KunigiArchive.Web2`, Blazor Web App, replacing the MVC site)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server reachable via `ConnectionStrings:DefaultConnection` in `appsettings.Development.json`

## Getting Started

```bash
dotnet restore
dotnet build
dotnet run --project src/KunigiArchive.Web
```

Server operations (layout, secrets, backups, setup scripts) are documented in [`ops/vps/README.md`](ops/vps/README.md).
