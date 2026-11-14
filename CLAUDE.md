# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

An archive site for "Το Κυνήγι Θησαυρού", an annual scavenger hunt in Rethymno, Crete. For each year's game it stores the teams that hosted and won it, the game's days, the puzzles (with question/answer media), posts, and media galleries. UI text is in Greek. Live at https://tokinigi.gr, with a handful of users (mostly the owner).

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
KunigiArchive.slnx          solution (folders: /src, /Solution Items)
Directory.Build.props       shared MSBuild settings (net10.0, nullable, implicit usings, version)
Directory.Packages.props    central package versions (projects use <PackageReference> without Version)
global.json                 SDK pin (10.0.x)
src/KunigiArchive.Web/             current MVC site, live at tokinigi.gr (own Dockerfile)
src/KunigiArchive.Domain/          new: domain model (empty for now)
src/KunigiArchive.Application/     new: use cases → Domain
src/KunigiArchive.Infrastructure/  new: EF Core, files, email → Application, Domain
src/KunigiArchive.Blazor/          new: Blazor Web App + REST API, beta.tokinigi.gr (own Dockerfile) → Application, Infrastructure
ops/vps/                           server scripts + README
docker-compose.yml                 live stack (proxy, db, web)
docker-compose.beta.yml            beta stack (own compose project, joins the live network)
```

The new projects are built **alongside** the MVC app: `KunigiArchive.Web` does not reference them. Features move over one endpoint at a time, and fixes go into both apps until the beta replaces the MVC site.

To add or upgrade a package, edit `Directory.Packages.props` (versions) and the project's `<PackageReference Include="..." />`. New projects go under `src/` (and later `tests/`) and pick up both props files automatically.

## Build & Run

```bash
dotnet build
dotnet run --project src/KunigiArchive.Web
dotnet watch run --project src/KunigiArchive.Web   # with hot reload
```

- Launch profiles: `http` → http://localhost:5000, `https` → https://localhost:5001
- Local dev needs a SQL Server reachable via `ConnectionStrings:DefaultConnection` in `appsettings.Development.json` (`localhost`, user `sa`).
- The dev SMTP password is in **user-secrets** (`dotnet user-secrets set EmailSettings:Password <value>` in `src/KunigiArchive.Web`).
- The dev admin is seeded from `AdminSeed` in `appsettings.Development.json` (`admin@test.com`). There's also a dev-only manager, `manager@test.com`.
- CI (`.github/workflows/ci.yml`) builds with `-warnaserror` (exempt: `NU1901–NU1904` vulnerable-package warnings and `CA1416`, see #28) and runs `dotnet test`. Reproduce locally:
  `dotnet build KunigiArchive.slnx -c Release -warnaserror -warnnotaserror:NU1901,NU1902,NU1903,NU1904,CA1416`

## Database & Migrations

SQL Server (EF Core SqlServer provider). Pending migrations apply automatically on startup.

```bash
dotnet ef migrations add <Name> --project src/KunigiArchive.Web --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/KunigiArchive.Web
```

Migrations live in `Data/Migrations` (namespace `KunigiArchive.Web.Data.Migrations`). In production the app connects as `kunigi_app` (db_owner on `KunigiArchive` only, because migrations run at startup).

## Configuration & Secrets

- `appsettings.json` holds **no secrets**: the connection string, `EmailSettings:Password` and `AdminSeed:Email/Password` are empty placeholders.
- Production values come from environment variables (`Section__Key`) in `/opt/kunigi/app.env` on the VPS, loaded through `env_file` in `docker-compose.yml`.
- The app refuses to start if `ConnectionStrings:DefaultConnection` is empty.
- `MediaDirectory` is where uploads are stored (`Utilities/MediaStorage.ResolveRoot`). It's required, and relative paths resolve against the project folder. In development it's `../../kunigi_media` (`<repo>/kunigi_media`). In production it's `/app/media`, bind-mounted from `/home/iperp/kunigi_media`.
- JSON comments are allowed in the appsettings files. `appsettings.Development.json` only contains overrides. Config arrays merge **by index** (e.g. `Serilog:WriteTo:0` overrides the Console sink).
- `EmailSettings` binds to `Settings/EmailSettings.cs`. `ApplicationUrl` is used to build links in emails.

## Deployment & Operations

- **Deploy** (manual, master only, separate workflows):
  - **Deploy web** (`deploy-web.yml`): copies the repo to `~iperp/kunigi-archive`, runs `docker compose up -d --build`, checks that https://tokinigi.gr returns 200.
  - **Deploy beta** (`deploy-beta.yml`): copies the repo to `~iperp/kunigi-beta`, runs `docker compose -f docker-compose.beta.yml up -d --build`, checks https://beta.tokinigi.gr. It never touches the live stack. Needs a DNS A record `beta.tokinigi.gr → 185.4.135.49`.
- CI builds the whole solution (required **Build** check) plus both Docker images (`Docker image (web)`, `Docker image (beta)`).
- **Schema ownership:** only `KunigiArchive.Web` runs migrations against the production DB. The new Infrastructure layer maps the existing schema and must not migrate until cutover.
- **Containers** (`docker-compose.yml`):
  - `kunigi_web`: the app, on :8080
  - `kunigi_db`: SQL Server 2022, volume `kunigi-archive_db_data`; port 1433 is published but firewalled to the WireGuard VPN only
  - `nginx-proxy` + `nginx-acme-companion`: TLS / reverse proxy
- **Everything about the server** (layout, secrets, SQL logins, WireGuard/SSMS, backups, setup scripts, change log) is in **`ops/vps/README.md`**. Host config is scripted in `ops/vps/` (`setup.sh`, `init-secrets.sh`, `wireguard.sh`, `backup/vps-backup.sh`, `sql/tune.sql`). Run them on the VPS with `sudo bash <script>` (#41).
- **Access**: `ssh kunigi-vps` (user `claude`, key `~/.ssh/kunigi_claude`, passwordless sudo).
- **Backups**: `vps-backup.sh` runs nightly at 02:00 and keeps 3 archives in `/opt/kunigi-backups` (DB `.bak` + media + env files). An off-site encrypted copy comes from the manual **Off-site backup** workflow.
- Never run `docker compose down -v`, and never rename the compose project or volumes.

## Architecture

**Stack:** .NET 10, ASP.NET Core MVC, EF Core (SQL Server), ASP.NET Core Identity, Serilog, SixLabors.ImageSharp 3.1.x (thumbnails), PDFtoImage (first PDF page → PNG on upload), LigerShark WebOptimizer (JS/CSS minification).

**Pattern:** service-oriented MVC.
- Controllers are thin. All business logic lives in scoped services.
- `Program.cs` only wires things up. Registration lives in `Extensions/ServiceCollectionExtensions.cs`:
  - `AddWebInfrastructure`: MVC, compression, WebOptimizer, rate limiting, upload limits
  - `AddPersistence`: EF Core and Identity
  - `AddApplicationServices`: the app's services and settings
  - Middleware is added with `UseRequestCorrelation()` / `UseSecurityHeaders()` (`Middleware/MiddlewareExtensions.cs`).
- Services resolve the current user via `IHttpContextAccessor` (`private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User!`). Controllers never pass `userId`/`isAdmin` into services.
- Code style follows `.editorconfig` (run `dotnet format`): explicit types instead of `var`, file-scoped namespaces.

**Auth:**
- Identity with two roles, `Admin` and `Manager`. A manager is linked to teams via `TeamManager`.
- A manager's access to a game = being a manager of the game's host team (the `HasGameAccess` / `HasTeamAccess` checks are duplicated per service, #25).
- Login path is `/auth/login`. Lockout is on. `/auth` is rate-limited by the `RateLimitPolicies.Auth` policy (60 req/min per IP; note #2, forwarded headers missing).
- Accounts are created by an admin, and the temporary password is emailed. Password reset uses a code kept in `IMemoryCache` (buggy, #14).

**Pipeline (`Program.cs`):**
- Middleware:
  - `RequestCorrelationMiddleware`: X-Correlation-Id → Serilog. Client-supplied ids are accepted only if they're `[A-Za-z0-9-]{1,64}`.
  - `SecurityHeadersMiddleware`, response compression (Brotli/Gzip), WebOptimizer, static files with a 30-day cache.
- Startup errors are logged as Fatal via the bootstrap Serilog logger (try/catch in `Program.cs`).
- A second static-file provider serves `MediaDirectory` at `/media`.
- `UseStatusCodePagesWithRedirects("/not-found")`, rate limiter, global `AutoValidateAntiforgeryTokenAttribute`.
- Request limits: 250 MB per request (Kestrel, and nginx `client_max_body_size` in `nginx-limits.conf`), 100 MB per file.
- Startup: `Database.MigrateAsync()`, then `DataSeed.Seed` (roles, admin from `AdminSeed`, dev manager, `GameDayType`s). The thumbnail backfill is commented out (#10).

**Routing:** entities have a `Slug` used in URLs (`/teams/{slug}`, `/games/{slug}`). `Utilities/SlugGenerator` transliterates Greek, strips diacritics, and checks reserved names.

**Media (`DirectoryService`):**
- Files live under `MediaDirectory`. Stored paths are web-relative (`media/...`).
- Layout:
  - `teams/{slug}/` — team media and `profile.*`
  - `games/{year}/` — game, game-day and puzzle media, plus the game's `profile.*`
  - `games/{year}/posts/{postId}/` — post media
- Images get a `thumb_<name>` copy (max 400px wide). Generated filenames are 11-char random IDs. PDFs are converted to PNG (first page).
- Changing a team slug or a game year moves the directory and rewrites the stored paths.
- No file-type allowlist yet (#13).

## Domain Model

- **`AppUser`** (`IdentityUser<long>`): has `TeamManager`s. **`AppRole`** (`IdentityRole<long>`).
- **`Team`**: `Name`, `Slug`, `IsActive`, `YearFounded?`, `Description?` (rich text), socials, `Image?`. Navigation: `Managers`, `MediaFiles`, `HostedGames`, `WonGames`.
- **`Game`**: `Year`, `Order`, `Slug`, `OrderTitle` ("{Order}ο Κυνήγι Θησαυρού"), `Title?`, `Description?`, `Image?`, `HostId`, `WinnerId?` (optional). Navigation: `Host`, `Winner?`, `GameDays`, `MediaFiles`, `Posts`. Host/Winner use `DeleteBehavior.Restrict`. `Year`/`Order` uniqueness is checked in code only (#23).
- **`GameDay`**: one per `GameDayType`, auto-created with the game and toggled with `IsEnabled`. Fields: `Title?`, `Description?`. Navigation: `Puzzles`, `GameDayMedia`.
- **`GameDayType`**: seeded values Χορός, Σάββατο, Κυριακή, Διαδικτυακό, Παιδικό, Εφηβικό.
- **`Puzzle`**: `GameDayId`, `Order`, `Group?` (puzzles in the same group form a "package"), `Question?`, `Answer?` (rich text). `PuzzleMedia.MediaType` is `"Q"` (question) or `"A"` (answer).
- **`Post`**: belongs to a **Game**. Fields: `Title?`, `Content` (rich text), `CreatedAt`. `PostMedia`.
- Media entities `TeamMedia`, `GameMedia`, `GameDayMedia`, `PuzzleMedia` and `PostMedia` all have `Path`, `OriginalFileName`, `FileName`, `TimeStamp`.

## Services

All are scoped, with interfaces in `Services/Interfaces`. They return `ServiceResponse` / `ServiceResponse<T>`.

- **`AccountService`**: paginated users, create account (emails a temporary password), account details, change role (can't change your own role; demoting a Manager removes its `TeamManager` rows), change email/password, password reset.
- **`TeamService`**: list/search, CRUD (delete is blocked if the team hosted or won games), manager assignment (Admin only), team media.
- **`GameService`**: list/search, create (validates Year/Order, creates the days and `games/{year}/`), details (public and managed), day toggles, game-day edit, game update (admin: slug/year/order/host/winner; manager: title/description/image), game media, game-day media.
- **`PuzzleService`**: puzzle management per day, create/edit with Q/A media, delete (compacts `Order`, dissolves single-member groups), delete puzzle media.
- **`PostService`**: posts per game (newest first), CRUD, post media.
- **`DirectoryService`**: create/delete/move directories, save files (+ thumbnail / PDF conversion), delete files, media status (admin page `/admin-dashboard/media-status`).
- **`EmailService`**: account-creation and password-reset emails (inline HTML, sent synchronously; #20 proposes background jobs).

## Controllers & Routes

Controllers call a service, check `Success`, set TempData through `AlertMessageHandler.SetMessage`, then redirect or return a view.

- **Home**: `/`, `/error`, `/not-found`, `/admin-dashboard` and `/admin-dashboard/media-status` [Admin]
- **Authentication** (`/auth`, rate-limited): `login`, `logout`, `forgot-password`, `reset-password`
- **Account** (`/account`): `management`, `create`, `details/{id}`, `details/{id}/change-role` [Admin]; `settings`, `change-email`, `change-password` [signed in]
- **Team** (`/teams`):
  - public: list, `{slug}`
  - [Admin]: `manage`, `create`, `{slug}/delete`, `{slug}/managers` (+ add/remove)
  - [Admin, Manager]: `{slug}/actions`, `{slug}/update`, `{slug}/media` (+ upload/delete)
  - [Manager]: `team-manager-dashboard`
- **Game** (`/games`):
  - public: list, `{slug}`, `{slug}/day/{id}` (enabled days only)
  - [Admin]: `manage`, `create`
  - [Admin, Manager], all under `{slug}/`:
    - `actions`, `update`, `days`, `media` (+ upload/delete)
    - `day/{gameDayId}/` `actions`, `update`, `media` (+ upload/delete), `puzzles` (+ create, edit, delete, delete media)
    - `posts` (+ create, edit, delete) and `posts/{postId}/media` (+ upload/delete)

## UI Patterns

- **Theme:** neo-brutalist custom Bootstrap 5.3 build (`wwwroot/lib/bootstrap/dist/css/style.css`):
  - square corners, 2px borders, hard offset shadows (`--reth-shadow-color`)
  - Space Grotesk headings
  - primary `#ff4d00`, secondary `#2e5cff`, warning `#ffd600`
  - light/dark toggle persisted in localStorage (`data-bs-theme`)
- **Dark mode:** never use `border-dark`/`border-black` or hard-coded colors in views. Use `.border-ink` and CSS variables (`var(--bs-border-color)`, `var(--bs-tertiary-bg)`, `var(--bs-card-bg)`). Component styles (timeline, media gallery, tiles) live in `site.css`.
- **Page headers:** set `ViewData["Banner"]` and the layout renders the banner. List pages render their own header row with `_SearchForm`.
- **Shared partials:**
  - `_Pagination` and `_SearchForm` take a `PageNavViewModel`
  - `_MediaGallery` (fancybox), `_PuzzleContent` (Q/A split), `_Notification` (TempData alerts)
- **Forms:** Bootstrap cards, `asp-for` tag helpers, validation spans, Back + Save aligned right. Team selects use `initCustomSelect(prefix)` (searchable dropdown driving an off-screen `<select>`).
- **Rich text:** Quill editor. HTML goes into a hidden input on submit and is rendered with `Html.Raw` **without sanitization (#12)**.
- **Role-specific UI:** `@if (User.IsInRole("Admin"))` / `("Manager")` in Razor.

## ServiceResponse Pattern

```csharp
ServiceResponse.Successful("message")
ServiceResponse.Failure("message")
ServiceResponse.Warning("message")   // Success = true, Warning severity
ServiceResponse<T>.Successful(data, "message")
ServiceResponse<T>.Failure("message")
```

Methods that can change a slug (`UpdateTeamAsync`, `UpdateGameAsync`) return `ServiceResponse<string>` with the final slug for the redirect.

## Known Issues

Open problems are tracked as GitHub issues (`gh issue list`). The highest-impact ones:
- #12 stored XSS
- #13 upload file types
- #14 password reset
- #15 vulnerable ImageSharp
- #2 forwarded headers
- #3 Data Protection keys not persisted (every deploy logs users out)
