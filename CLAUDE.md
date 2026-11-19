# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

An archive site for "Το Κυνήγι Θησαυρού", an annual scavenger hunt in Rethymno, Crete. For each year's game it stores the teams that hosted and won it, the game's days, the puzzles (with question/answer media), posts, and media galleries. UI text is in Greek. Live at https://tokinigi.gr, with a handful of users (mostly the owner).

Since 2026-11-19 the site is `src/KunigiArchive.Web` (Blazor, clean layers). It replaced an older ASP.NET Core MVC app; that app's frozen database and media are still on the server for a rollback until they're removed (#89).

## Working Agreements

- **Never commit, push, open PRs, or create/delete remote branches unless the owner explicitly says so.** Make changes in the working tree and report what's ready.
- `master` is protected: changes go in through a PR, the **Build** check must pass, and squash merge is the only option. Work on branches.
- **GitHub only builds (CI) and deploys (manual).** Package and .NET updates are done locally. There's no Dependabot and no package scanning in CI.
- Stay on **.NET 10 (LTS)**; `global.json` pins SDK 10.0.x. Take the latest 10.0.x patches, never 11.x packages.
- **SixLabors.ImageSharp stays on 3.1.x.** 3.2+ and 4.x are under a commercial license.
- **Never lose production data** (SQL Server DB and uploaded media). Take or verify a backup before touching the DB volume, the media folder, compose volumes, or migrations. Downtime is acceptable.
- Track problems as labeled GitHub issues (`gh issue create`). Type labels: `bug`, `security`, `infrastructure`, `data-safety`, `tech-debt`, `documentation`, `enhancement`. Priority labels: `priority: high|medium|low`.

## Solution Layout

```
KunigiArchive.slnx                solution (folders: /src, /Solution Items)
Directory.Build.props             shared MSBuild settings (net10.0, nullable, implicit usings, version)
Directory.Packages.props          central package versions, with transitive pinning (projects use <PackageReference> without Version)
global.json                       SDK pin (10.0.x)
src/KunigiArchive.Domain/         entities and rules, repository interfaces
src/KunigiArchive.Application/    use cases (one handler per action), queries, authorization → Domain
src/KunigiArchive.Infrastructure/ EF Core, migrations, Identity, media storage, email → Application, Domain
src/KunigiArchive.Web/            the site: Blazor pages, REST API (/api), middleware (own Dockerfile) → Application, Infrastructure
src/KunigiArchive.Import/         one-off import from the old MVC database + media (2026); removed with the old data
ops/vps/                          server scripts + runbook (README.md)
docker-compose.yml                production stack (proxy, acme, db, web)
docker-compose.dev.yml            local SQL Server (+ the site in a container with --profile app)
```

To add or upgrade a package, edit `Directory.Packages.props` (versions) and the project's `<PackageReference Include="..." />`.

## Build & Run

```bash
docker compose -f docker-compose.dev.yml up -d                       # SQL Server on 127.0.0.1:1433 (sa / Your_password123)
dotnet run --project src/KunigiArchive.Web --launch-profile http    # http://localhost:5256
dotnet watch --project src/KunigiArchive.Web                         # with hot reload
docker compose -f docker-compose.dev.yml --profile app up -d --build # the production image, on http://localhost:8080
```

- Development settings are in `appsettings.Development.json`: database `KunigiArchiveV2` on localhost, uploads in `<repo>/kunigi_media_v2`, emails saved as `.eml` files in `<repo>/kunigi_mail_v2` (no SMTP needed), Swagger at `/swagger`.
- On an empty database `IdentitySeeder` creates the roles and a first admin from `AdminSeed:Email/Password` (user-secrets locally, `web.env` on the server); in development it also creates `manager@test.com`.
- CI (`.github/workflows/ci.yml`) builds with `-warnaserror` (exempt: `NU1901–NU1904` vulnerable-package warnings, #15), runs `dotnet test` (there are no test projects), and builds the Docker image. Reproduce locally:
  `dotnet build KunigiArchive.slnx -c Release -warnaserror -warnnotaserror:NU1901,NU1902,NU1903,NU1904`
- Code style follows `.editorconfig` (run `dotnet format`): explicit types instead of `var`, file-scoped namespaces.

## Database & Migrations

SQL Server through EF Core (`KunigiArchiveDbContext`). Migrations live in `src/KunigiArchive.Infrastructure/Persistence/Migrations` and apply automatically on startup (`InitializeDatabaseAsync`, then `IdentitySeeder`).

```bash
dotnet ef migrations add <Name> --project src/KunigiArchive.Infrastructure --startup-project src/KunigiArchive.Web --output-dir Persistence/Migrations
```

- The production database is `KunigiArchiveV2` (a historical name: it was the new app's while the old site still ran). The site connects as `kunigi_beta` (also historical), `db_owner` on that database only.
- `import.IdMap` (old long id → new Guid) is left from the import; harmless, goes with the Import project.

## Configuration & Secrets

- `appsettings.json` holds **no secrets**: the connection string (`ConnectionStrings:KunigiArchive`), `EmailSettings:Password` and `AdminSeed:Email/Password` are empty placeholders. JSON comments are allowed.
- Production values come from environment variables (`Section__Key`) in `/opt/kunigi/web.env` on the VPS, loaded through `env_file` in `docker-compose.yml`. Compose itself sets `DataProtection__KeysPath=/app/keys` and `LogFile__Path=/app/logs/web-.log`.
- `MediaStorage:RootPath` is where uploads go (`/app/media` in production, bind-mounted from `/home/iperp/kunigi_web_media`).
- `EmailSettings` binds to `Infrastructure/Email/EmailOptions.cs`; `ApplicationUrl` builds the links in emails. `PickupDirectory` (development) writes `.eml` files instead of sending.
- `Robots:NoIndex` (off in production) sends `X-Robots-Tag: noindex` and makes `/robots.txt` disallow everything, for a test copy.
- Logging: Serilog sinks are set in code (`AddSerilogLogging`), because config arrays merge by index: coloured text in development, one JSON object per line in production, and rotating files (daily, 10 MB, 30 kept) where `LogFile:Path` is set. appsettings only holds levels and properties. SQL commands are not logged.
- Data Protection keys persist in `DataProtection:KeysPath`. The application name stays `"KunigiArchive.Web2"` (see `AddHosting`): changing it voids the stored keys and signs everyone out.

## Deployment & Operations

- **Deploy web** (`deploy-web.yml`, manual, master only): streams the repo to `~iperp/kunigi-archive` as a tar over SSH and swaps the folder in (so deleted files don't linger, #45), runs `docker compose up -d --build`, checks that https://tokinigi.gr returns 200.
- **Off-site backup** (`backup.yml`, manual, currently disabled): downloads the newest backup, checks it, stores it encrypted as an artifact (#38).
- **Containers** (`docker-compose.yml`, compose project `kunigi-archive`):
  - `kunigi_web`: the site, on :8080, runs as uid 1654; mounts `kunigi_web_media`, `kunigi_web_keys`, `kunigi_web_logs`
  - `kunigi_db`: SQL Server 2022, volume `kunigi-archive_db_data`; port 1433 is published but firewalled to the WireGuard VPN only
  - `nginx-proxy` + `nginx-acme-companion`: TLS / reverse proxy (HSTS comes from here)
- **Everything about the server** (layout, secrets, SQL logins, WireGuard/SSMS, backups, restore, rollback, change log) is in **`ops/vps/README.md`**. Host config is scripted in `ops/vps/` (`setup.sh`, `init-secrets.sh`, `rotate-smtp.sh`, `wireguard.sh`, `backup/vps-backup.sh`, `sql/tune.sql`). Run them on the VPS with `sudo bash <script>` (#41).
- **Access**: `ssh kunigi-vps` (user `claude`, key `~/.ssh/kunigi_claude`, passwordless sudo).
- **Backups**: `vps-backup.sh` runs nightly at 02:00 and keeps 3 archives in `/opt/kunigi-backups` (both databases, `kunigi_web_media`, `kunigi_web_keys`, the old site's media, env files). A failure is logged as `BACKUP FAILED` in `/var/log/kunigi-backup.log`.
- Never run `docker compose down -v`, and never rename the compose project or volumes.

## Architecture

**Layers.** Domain (entities with their rules, no dependencies) ← Application (handlers) ← Infrastructure (EF Core, Identity, files, email) ← Web (Blazor pages and API controllers). Pages and controllers call Application handlers only.

**Application.** One folder per use case with a `…Handler` (and a command/query record where needed), registered in `Application/DependencyInjection.cs`.
- Writes go through domain entities and repositories (`ITeamRepository`, …) plus `IUnitOfWork`; reads go through query interfaces (`ITeamQueries`, …) implemented in `Infrastructure/Persistence/Queries`, returning DTO records.
- Authorization lives in the handlers: `ICurrentUser` (`EnsureAdmin`, …), `GameAccess` for a game's editors. Failures are exceptions from `Abstractions/Exceptions` (`NotFoundException`, `InvalidRequestException`, `ConflictException`, …), which pages turn into messages and the API into problem details (`GlobalExceptionHandler`).
- Lists use keyset paging (`KeysetCursor`); the admin lists load everything in one page so column sorting covers it (#80).
- Changing a team or game slug records the old one (`SlugRedirects`); old URLs answer with a 301.

**Web (Blazor, static server rendering with enhanced navigation).**
- Pages in `Components/Pages`, shared parts in `Components/Shared`. Forms are plain `<form method="post">` / `EditForm` with `[SupplyParameterFromForm]`, models in `Forms/`; `FormPage` gives `Done`/`Reload` with a flash message (`Flash`).
- The REST API is in `Controllers/` with request/response records in `Contracts/` and mappers in `Mappers/`.
- Auth: Identity cookies, roles `Admin` and `Manager` (a manager edits the teams they manage and the games those teams hosted). Disabled accounts can't sign in (`DisabledUserConfirmation`); a temporary password forces a password change (`MustChangePasswordMiddleware`, #24). The security stamp is re-checked every minute.
- Middleware (`Program.cs`, in order): forwarded headers, correlation id (`X-Correlation-Id` → log property `CorrelationId`), Serilog request log, security headers with a per-request CSP nonce (`SecurityHeadersMiddleware`), response compression, status code pages, media files at `/media`, routing, rate limiting (auth pages), authentication, authorization, must-change-password, antiforgery.
- `site.js` handles the rich-text editor (Quill), the gallery (GLightbox), searchable selects, sortable tables, `data-confirm` buttons and busy (spinner) submit buttons. Scripts and fonts are self-hosted (CSP: `'self'` only).
- `/robots.txt` and `/sitemap.xml` are endpoints (`WebApplicationExtensions.MapSearchEngineFiles`); pages set description, canonical and Open Graph tags with `<PageMeta>`.

**Media (`LocalMediaStorage`).** Uploads are checked by content (images, PDF, MP4); images are shrunk and stored as WebP with a `thumb_` copy, PDFs become an image of their first page. Stored paths are web-relative (`media/teams/{teamId}/…`, `media/games/{gameId}/days/{dayId}/puzzles/{puzzleId}/…`). Files are named with random ids.

**Rich text.** Quill in the browser; `RichTextSanitizer` (HtmlSanitizer allowlist) cleans it on save and again on display, drops empty blocks at the start and end, and treats text-less HTML as empty.

**Email.** `IAccountEmails` queues account emails (account created, temporary password, password reset) in memory; `EmailSendingService` sends them over SMTP (Mailtrap) with retries (#20). Bodies and addresses are never logged.

## UI Patterns

- **Theme:** paper and ink, defined as CSS variables in `wwwroot/app.css` (`--k-paper`, `--k-ink`, `--k-accent`, `--k-rule`, …), with a light/dark toggle (`theme.js`). Headings in GFS Didot, text in Source Sans 3 (self-hosted, `wwwroot/fonts`). The base size is 18px, the page 1200px wide.
- Page-specific styles are scoped (`*.razor.css`); site-wide ones are in `app.css`. Use the CSS variables, never hard-coded colours.
- Details pages leave out empty sections. Puzzles show the question with the answer in a closed card (`PuzzleView`).
- Forms sit in a centered `k-form` card; actions on the right.

## Known Issues

Open problems are tracked as GitHub issues (`gh issue list`). Notable ones: #15 (ImageSharp 3.1.12 advisories, no free fix), #17/#18/#19 (server updates and hardening), #38 (restore rehearsal, backup alerts, off-site copy), #89 (the switch and the old data's removal).
