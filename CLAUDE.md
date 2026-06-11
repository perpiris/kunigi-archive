# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

An archive site for "Το Κυνήγι Θησαυρού" — an annual scavenger hunt game in Rethymno, Crete. It stores each year's game, the teams that hosted and won it, the game's days/events, puzzles (with question/answer media), posts, and media galleries. UI text is in Greek.

## Build & Run

```bash
dotnet build
dotnet run --project KunigiArchive.Web
dotnet watch run --project KunigiArchive.Web   # with hot reload
```

Launch profiles (from `launchSettings.json`):
- `http`: `http://localhost:5000`
- `https`: `https://localhost:5001`

Note: SixLabors.ImageSharp is pinned to 3.x — version 4.x requires a paid build-time license key. Do not bump it without one.

## Database Migrations

Migrations apply automatically on startup. To manage manually:

```bash
dotnet ef migrations add <Name> --project KunigiArchive.Web --output-dir Data/Migrations
dotnet ef database update --project KunigiArchive.Web
```

The SQLite database file is `kunigi_archive.db` (local, not committed). All migrations live in `Data/Migrations` (namespace `KunigiArchive.Web.Data.Migrations`).

## Architecture

**Stack:** .NET 10, ASP.NET Core MVC, Entity Framework Core (SQLite), ASP.NET Core Identity, Serilog, SixLabors.ImageSharp (thumbnails), LigerShark WebOptimizer (JS/CSS minification)

**Pattern:** Service-oriented MVC. Controllers are thin — all business logic lives in scoped services. Services resolve the current user via `IHttpContextAccessor` (`private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User!`). Controllers never extract `userId` or `isAdmin` to pass into services — the service reads `User` internally.

**Auth:** Role-based via ASP.NET Core Identity. Two roles: `Admin` and `Manager`. Managers are linked to specific teams via the `TeamManager` join entity. A manager's access to a game is determined by whether they manage that game's host team. Cookie `LoginPath` is `/auth/login`. Login uses lockout (`lockoutOnFailure: true`). Auth endpoints are rate-limited via the `fixed` policy (60 req/min per IP).

**Pipeline (`Program.cs`):** `RequestCorrelationMiddleware` (X-Correlation-Id → Serilog `LogContext`), `SecurityHeadersMiddleware` (nosniff, X-Frame-Options DENY, referrer/permissions policies), response compression (Brotli/Gzip), WebOptimizer minification, static files with 30-day cache headers, `UseStatusCodePagesWithRedirects("/not-found")`, rate limiter, global `AutoValidateAntiforgeryTokenAttribute`. On startup: pending migrations apply, `DataSeed.Seed` runs, and `DirectoryService.GenerateMissingThumbnailsAsync()` backfills thumbnails.

**Routing:** Entities expose a `Slug` property used for URL segments (e.g., `/teams/team-slug`, `/games/game-slug`). The `SlugGenerator` utility in `Utilities/` handles slug creation from Greek or Latin text (transliteration, diacritics removal, reserved-name checks).

**Media:** File uploads are stored under `wwwroot/media/`. `DirectoryService` manages path resolution, file saving, directory create/delete/move, and thumbnail generation (images get a `thumb_<name>` copy, max 400px wide, via ImageSharp). Media entities (`TeamMedia`, `GameMedia`, `GameDayMedia`, `PuzzleMedia`) store file references separately from their parent entities. Stored paths are web-relative (`media/...`). Game media (including game-day and puzzle media) live under `games/{year}/`; team media under `teams/{slug}/`. Profile images are saved as `profile.*` in those folders. When an admin changes a team's slug or a game's year, the directory is moved and all stored media paths are rewritten to match.

**Seeding:** `Data/DataSeed.cs` seeds roles (Admin, Manager), a default admin user, a dev-only manager user (`manager@test.com`, Development environment only), and `GameDayType` records. Runs on every startup (idempotent).

**Configuration:** `appsettings.json` holds production values; `appsettings.Development.json` overrides for local dev. `EmailSettings` is bound from config to `Settings/EmailSettings.cs`.

## Domain Model

### Entities

- **`AppUser`** — extends `IdentityUser<long>`. Has `ICollection<TeamManager>`.
- **`AppRole`** — extends `IdentityRole<long>`.
- **`Team`** — `TeamId`, `Name`, `Slug`, `IsActive`, `YearFounded?`, `Description?`, `Facebook?`, `Instagram?`, `Youtube?`, `Website?`, `Image?`. Navigation: `Managers`, `MediaFiles`, `HostedGames`, `WonGames`.
- **`TeamManager`** — join entity linking `AppUser` ↔ `Team` via `AppUserId` + `TeamId`.
- **`TeamMedia`** — `TeamMediaId`, `TeamId`, `Path`, `OriginalFileName`, `FileName`, `TimeStamp`.
- **`Game`** — `GameId`, `Year`, `Order`, `Slug`, `OrderTitle`, `Title?`, `Description?`, `Image?`, `HostId`, `WinnerId`. Navigation: `Host`, `Winner`, `GameDays`, `MediaFiles`, `Posts`.
- **`GameDay`** — `GameDayId`, `Title?`, `Description?`, `Image?`, `IsEnabled`, `GameTypeId`, `GameId`. Navigation: `GameDayType`, `Game`, `Puzzles`, `GameDayMedia`.
- **`GameDayType`** — `GameDayTypeId`, `Label`, `Slug`. Seeded values: Χορός, Σάββατο, Κυριακή, Διαδικτυακό, Παιδικό, Εφηβικό.
- **`GameMedia`** — `GameMediaId`, `GameId`, `Path`, `OriginalFileName`, `FileName`, `TimeStamp`.
- **`GameDayMedia`** — `GameDayMediaId`, `GameDayId`, `Path`, `OriginalFileName`, `FileName`, `TimeStamp`.
- **`Puzzle`** — `PuzzleId`, `GameDayId`, `Order`, `Group?`, `Question?`, `Answer?`. Navigation: `GameDay`, `MediaFiles`.
- **`PuzzleMedia`** — `PuzzleMediaId`, `PuzzleId`, `MediaType` (`"Q"` question / `"A"` answer), `Path`, `OriginalFileName`, `FileName`, `TimeStamp`.
- **`Post`** — `PostId`, `GameId`, `Content`, `CreatedAt`. Navigation: `Game`. (Posts belong to a **Game**, not a GameDay.)

### Core Relationships
- A `Game` has one host `Team` (`HostId`) and one winner `Team` (`WinnerId`) — both `DeleteBehavior.Restrict`
- A `Game` auto-creates one `GameDay` per `GameDayType` on creation; days are toggled visible via `IsEnabled`
- A `GameDay` contains ordered `Puzzle` entries (optionally grouped via `Group`)
- A `Game` contains `Post` entries
- A manager's game access = being a `TeamManager` of the game's host `Team`
- `Year` and `Order` are unique across games (validated in service)

## Services

All services are scoped and registered in `Program.cs`.

### `AccountService` (`IAccountService`)
- `GetPaginatedAccountsAsync` — paginated user list with roles
- `CreateAccountAsync` — creates user with role, sends creation email with temp password
- `GetAccountDetailsAsync` — current user's info + role
- `GetAccountByIdAsync` — user info + role + managed teams (admin account details page)
- `ChangeRoleAsync` — switches a user's role; blocks changing your own role; removes `TeamManager` records when demoting a Manager
- `ChangeEmailAsync` / `ChangePasswordAsync` — account settings
- `GetAvailableManagersAsync` — users in Manager role, eligible to be team managers
- `InitiatePasswordResetAsync` / `ResetPasswordAsync` — code-based reset via memory cache (1h expiry)

### `TeamService` (`ITeamService`)
- `GetPaginatedTeamsAsync` — paginated/searchable team list
- `CreateTeamAsync` — creates team + `wwwroot/media/teams/{slug}/` directory
- `GetTeamBySlugAsync(slug, includeFullDetails, isForPublicView)` — always loads `HostedGames` + `Managers`; loads `WonGames` + `MediaFiles` only when `includeFullDetails = true`; skips access check when `isForPublicView = true`
- `UpdateTeamAsync` — returns `ServiceResponse<string>` with the team's final slug. Admin can update name + slug (slug is sanitized through `SlugGenerator`; on slug change the media directory is moved and stored paths rewritten); all roles update description, social links, image, active status
- `DeleteTeamAsync` — blocked if team has hosted/won games; removes managers, media records, and directory
- `GetTeamManagerViewModelAsync` / `AddTeamManagerAsync` / `RemoveTeamManagerAsync` — manager assignment (Admin only)
- `GetAllUserManagedTeams` — teams the current user manages
- `GetTeamMediaViewModelAsync` / `UploadTeamMediaAsync` / `DeleteTeamMediaAsync` — media management
- Access check: `HasTeamAccess(team)` — true if Admin or listed in `team.Managers`

### `GameService` (`IGameService`)
- `GetPaginatedGamesAsync` — paginated/searchable game list
- `PrepareGameCreateViewModelViewModel` — builds select lists for host/winner dropdowns
- `CreateGameAsync` — validates uniqueness of Year/Order, creates game + GameDays for all GameDayTypes + `wwwroot/media/games/{year}/` directory
- `GetGameBySlugAsync(slug, includeFullDetails, isForPublicView)` — access-checked unless public; loads full nav when `includeFullDetails = true`
- `GetGameDayDetailsForPublicViewAsync(slug, gameDayId)` — public game-day page (puzzles with Q/A media, day media); only when day `IsEnabled`
- `GetGameDaysViewModelAsync` / `UpdateGameDaysAsync` — toggle `IsEnabled` per day
- `GetGameDayActionsAsync` / `PrepareGameDayUpdateAsync` / `UpdateGameDayAsync` — game-day title/description editing
- `PrepareGameUpdateViewModelAsync(slug, viewModel?)` — access-checked; builds select lists for admin only
- `UpdateGameAsync(viewModel, image, modelState)` — returns `ServiceResponse<string>` with final slug. Admin can update Year/Order/HostId/WinnerId/Slug (on year change the media directory is moved and all stored paths — game image, game/game-day/puzzle media — are rewritten); all roles can update Description + Image
- `GetGameMediaViewModelAsync` / `UploadGameMediaAsync` / `DeleteGameMediaAsync` — game media
- `GetGameDayMediaViewModelAsync` / `UploadGameDayMediaAsync` / `DeleteGameDayMediaAsync` — game-day media
- Access check: `HasGameAccess(game)` — true if Admin or listed in `game.Host.Managers`

### `PuzzleService` (`IPuzzleService`)
- `GetPuzzleManagementAsync(gameSlug, gameDayId)` — puzzle list for a day
- `PrepareCreateViewModelAsync` / `CreatePuzzleAsync` — new puzzle gets `Order = max + 1`; saves question/answer media (`MediaType` `"Q"`/`"A"`)
- `GetPuzzleEditAsync` / `UpdatePuzzleAsync` — edit question/answer/group + add media
- `DeletePuzzleAsync` — deletes puzzle + media files, re-compacts `Order` of remaining puzzles, dissolves single-member groups
- All methods access-checked via the game's host team

### `PostService` (`IPostService`)
- `GetPostManagementAsync(gameSlug)` — posts for a game, newest first
- `PrepareCreateViewModelAsync` / `CreatePostAsync` / `GetPostEditAsync` / `UpdatePostAsync` / `DeletePostAsync`
- All methods access-checked via the game's host team

### `DirectoryService` (`IDirectoryService`)
- `CreateDirectory(path)` / `DeleteDirectory(path)` / `MoveDirectory(source, destination)` — all relative to `wwwroot/media/`
- `SaveFileAsync(file, storagePath, desiredFileName?)` — creates the target directory if missing; if `desiredFileName` provided, deletes existing files (and thumbnails) with same prefix first; generates a thumbnail for images; returns web-relative path `media/...`
- `DeleteFile(path)` — deletes file + its thumbnail
- `GenerateMissingThumbnailsAsync()` — startup backfill
- Max file size: 100 MB. Generated filenames: 11-char alphanumeric short ID. Thumbnails: `thumb_<filename>`, max width 400px.

### `EmailService` (`IEmailService`)
- `SendAccountCreationEmailAsync` — sends temp password to new user
- `SendPasswordResetEmailAsync` — sends reset link (`/auth/reset-password?code=...&email=...`)

## Controllers

Controllers are thin. They call a service method, check `serviceResponse.Success`, set a TempData message via `AlertMessageHandler.SetMessage`, and redirect or return a view. They never resolve user identity to pass into services.

### `HomeController`
- `GET /` → `Index`
- `GET /error` → `Error` (sets 500)
- `GET /not-found` → `NotFound` (sets 404)
- `GET /admin-dashboard` → `AdminDashboard` [Admin]

### `AuthenticationController` (`/auth`, rate-limited)
- `GET/POST /auth/login` → `Login`
- `POST /auth/logout` → `Logout`
- `GET/POST /auth/forgot-password` → `ForgotPassword`
- `GET/POST /auth/reset-password` → `ResetPassword`

### `AccountController` (`/account`, [Authorize])
- `GET /account/management` → `AccountManagement` [Admin]
- `GET/POST /account/create` → `AccountCreate` [Admin]
- `GET /account/details/{id}` → `AccountDetails` [Admin]
- `POST /account/details/{id}/change-role` → `ChangeRole` [Admin]
- `GET /account/settings` → `AccountSettings`
- `POST /account/change-email` → `ChangeEmail`
- `POST /account/change-password` → `ChangePassword`

### `TeamController` (`/teams`)
- `GET /teams` → `TeamList` (public)
- `GET /teams/manage` → `TeamManagement` [Admin]
- `GET/POST /teams/create` → `TeamCreate` [Admin]
- `GET /teams/{slug}` → `TeamDetails` (public, full details)
- `GET /teams/{slug}/actions` → `TeamActions` [Admin, Manager]
- `GET/POST /teams/{slug}/update` → `TeamUpdate` [Admin, Manager]
- `POST /teams/{slug}/delete` → `DeleteTeam` [Admin]
- `GET /teams/{slug}/managers` → `TeamManagers` [Admin]
- `POST /teams/{slug}/managers/add` / `POST /teams/{slug}/managers/remove` [Admin]
- `GET /teams/{slug}/media` → `TeamMedia` [Admin, Manager]
- `POST /teams/{slug}/media/upload` / `POST /teams/{slug}/media/{teamMediaId}/delete` [Admin, Manager]
- `GET /teams/team-manager-dashboard` → `TeamManagerDashboard` [Manager]

### `GameController` (`/games`)
- `GET /games` → `GameList` (public)
- `GET /games/manage` → `GameManagement` [Admin]
- `GET/POST /games/create` → `GameCreate` [Admin]
- `GET /games/{slug}` → `GameDetails` (public)
- `GET /games/{slug}/day/{id}` → `GameDayDetails` (public, enabled days only)
- `GET /games/{slug}/actions` → `GameActions` [Admin, Manager]
- `GET/POST /games/{slug}/update` → `GameUpdate` [Admin, Manager] — admin: slug/year/order/host/winner + description/image; manager: description/image only
- `GET/POST /games/{slug}/days` → `GameDays` / `UpdateGameDays` [Admin, Manager]
- `GET /games/{slug}/media` + upload/delete [Admin, Manager]
- `GET /games/{slug}/day/{gameDayId}/actions|update|media` (+ media upload/delete) [Admin, Manager]
- `GET /games/{slug}/day/{gameDayId}/puzzles` + create/edit/delete [Admin, Manager]
- `GET /games/{slug}/posts` + create/edit/delete [Admin, Manager]

## UI Patterns

**Theme:** Neo-brutalist design system on a custom Bootstrap 5.3 build (`wwwroot/lib/bootstrap/dist/css/style.css`): square corners everywhere, 2px borders, hard offset shadows (`--reth-shadow-color`, black in light mode / white in dark mode), Space Grotesk for headings/buttons, brand colors (primary `#ff4d00` orange, secondary `#2e5cff` blue, warning `#ffd600` yellow). Light/dark theme toggle persisted in localStorage (`data-bs-theme`).

**Dark-mode rule:** never use `border-dark`/`border-black` or hardcoded colors (`#fff`, `#f5f5f5`, `#212529`) in views — use the `.border-ink` utility and CSS variables (`var(--bs-border-color)`, `var(--bs-tertiary-bg)`, `var(--bs-card-bg)`) from `site.css`. Component styles (timeline, media gallery, tiles) live in `site.css`, not inline `<style>` blocks.

**Page headers:** set `ViewData["Banner"]` and the layout renders a standard page banner (uppercase display heading + bottom rule). List pages render their own header row with a `_SearchForm` partial instead.

**Shared partials:** `_Pagination` and `_SearchForm` both take a `PageNavViewModel` (Controller, Action, CurrentPage, TotalPages, PageSize, SearchQuery). `_MediaGallery` (fancybox grid), `_PuzzleContent` (Q/A split), `_Notification` (TempData alerts).

**Forms:** Bootstrap 5 cards with `asp-for` tag helpers, validation spans, Back + Save buttons aligned right.

**Custom dropdowns:** Team select fields use a custom `initCustomSelect(prefix)` JS function with search. The real `<select>` is positioned off-screen; a visible `<input>` + dropdown panel drives UX.

**Rich text:** Quill editor (`~/lib/quill/quill.snow.css` + `quill.js`) for description fields. On form submit, the editor's HTML is written to a hidden `<input>`.

**Notifications:** `AlertMessageHandler` writes to TempData keys `Success`, `Warning`, `Error`. A shared `_Notification` partial renders them.

**Pagination + search:** Views pass `pageNumber`, `pageSize`, `searchQuery` query params. `PaginatedViewModel<T>` carries `Items`, `CurrentPage`, `TotalPages`, `PageSize`.

**Admin-only sections in shared views:** Use `@if (User.IsInRole("Admin"))` / `@if (User.IsInRole("Manager"))` in Razor to conditionally show fields or cards (e.g., slug/year/order fields in `GameUpdate`, hosted games section in `TeamActions`).

## ServiceResponse Pattern

```csharp
ServiceResponse.Successful("message")
ServiceResponse.Failure("message")
ServiceResponse.Warning("message")   // Success = true, Warning severity
ServiceResponse<T>.Successful(data, "message")
ServiceResponse<T>.Failure("message")
```

Controllers check `serviceResponse.Success`, call `AlertMessageHandler.SetMessage(TempData, serviceResponse)`, then redirect or return a view with `serviceResponse.Data`. Update methods that can change a slug (`UpdateTeamAsync`, `UpdateGameAsync`) return `ServiceResponse<string>` carrying the final slug for the redirect.
