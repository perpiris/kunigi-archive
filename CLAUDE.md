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

**Pattern:** Service-oriented MVC. Controllers are thin — all business logic lives in scoped services. Services resolve the current user via `IHttpContextAccessor` (`private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User!`). Controllers never extract `userId` or `isAdmin` to pass into services — the service reads `User` internally.

**Auth:** Role-based via ASP.NET Core Identity. Two roles: `Admin` and `Manager`. Managers are linked to specific teams via the `TeamManager` join entity. A manager's access to a game is determined by whether they manage that game's host team.

**Routing:** Entities expose a `Slug` property used for URL segments (e.g., `/teams/team-slug`, `/games/game-slug`). The `SlugGenerator` utility in `Utilities/` handles slug creation from Greek or Latin text.

**Media:** File uploads are stored under `wwwroot/media/`. `DirectoryService` manages path resolution and file saving. Media entities (`TeamMedia`, `GameMedia`, `GameDayMedia`, `PuzzleMedia`) store file references separately from their parent entities. Game profile images are saved under `games/{year}/profile.*`. Team profile images are saved under `teams/{slug}/profile.*`.

**Seeding:** `Data/DataSeed.cs` seeds roles (Admin, Manager), a default admin user, a dev manager user, and `GameDayType` records. Runs on every startup (idempotent).

**Configuration:** `appsettings.json` holds production values; `appsettings.Development.json` overrides for local dev. `EmailSettings` is bound from config to `Settings/EmailSettings.cs`.

## Domain Model

### Entities

- **`AppUser`** — extends `IdentityUser<long>`. Has `ICollection<TeamManager>`.
- **`AppRole`** — extends `IdentityRole<long>`.
- **`Team`** — `TeamId`, `Name`, `Slug`, `IsActive`, `YearFounded?`, `Description?`, `Facebook?`, `Instagram?`, `Youtube?`, `Website?`, `Image?`. Navigation: `Managers`, `MediaFiles`, `HostedGames`, `WonGames`.
- **`TeamManager`** — join entity linking `AppUser` ↔ `Team` via `AppUserId` + `TeamId`.
- **`TeamMedia`** — `TeamMediaId`, `TeamId`, `Path`, `OriginalFileName`, `FileName`, `TimeStamp`.
- **`Game`** — `GameId`, `Year`, `Order`, `Slug`, `OrderTitle`, `Title?`, `Description?`, `Image?`, `HostId`, `WinnerId`. Navigation: `Host`, `Winner`, `GameDays`, `MediaFiles`.
- **`GameDay`** — `GameDayId`, `Title?`, `Description?`, `IsEnabled`, `GameTypeId`, `GameId`. Navigation: `GameDayType`, `Game`, `Puzzles`, `GameDayMedia`, `Posts`.
- **`GameDayType`** — `GameDayTypeId`, `Label`, `Slug`. Seeded values: Χωρός, Σάββατο, Κυριακή, Διαδικτυακό, Παιδικό, Εφηβικό.
- **`GameMedia`** — `GameMediaId`, `GameId`, `Path`, `OriginalFileName`, `FileName`, `TimeStamp`.
- **`GameDayMedia`** — `GameDayMediaId`, `GameDayId`, `Path`, `OriginalFileName`, `FileName`, `TimeStamp`.
- **`Puzzle`** — `PuzzleId`, `GameDayId`, `Order`, `Group?`, `Question?`, `Answer?`. Navigation: `GameDay`, `MediaFiles`.
- **`PuzzleMedia`** — `PuzzleMediaId`, `PuzzleId`, `MediaType`, `Path`, `OriginalFileName`, `FileName`, `TimeStamp`.
- **`Post`** — `PostId`, `GameDayId`, `Content`, `CreatedAt`. Navigation: `GameDay`.

### Core Relationships
- A `Game` has one host `Team` (`HostId`) and one winner `Team` (`WinnerId`)
- A `Game` auto-creates one `GameDay` per `GameDayType` on creation
- A `GameDay` contains `Puzzle` entries and `Post` entries
- A manager's game access = being a `TeamManager` of the game's host `Team`

## Services

All services are scoped and registered in `Program.cs`.

### `AccountService` (`IAccountService`)
- `GetPaginatedAccountsAsync` — paginated user list
- `CreateAccountAsync` — creates user with role, sends creation email with temp password
- `GetAccountDetailsAsync` — user info + role
- `ChangeEmailAsync` / `ChangePasswordAsync` — account settings
- `GetAvailableManagersAsync` — users eligible to be team managers
- `InitiatePasswordResetAsync` / `ResetPasswordAsync` — token-based reset via memory cache (1h expiry)

### `TeamService` (`ITeamService`)
- `GetPaginatedTeamsAsync` — paginated/searchable team list
- `CreateTeamAsync` — creates team + `wwwroot/media/teams/{slug}/` directory
- `GetTeamBySlugAsync(slug, includeFullDetails, isForPublicView)` — always loads `HostedGames` + `Managers`; loads `WonGames` + `MediaFiles` only when `includeFullDetails = true`; skips access check when `isForPublicView = true`
- `UpdateTeamAsync` — admin can update name + slug; managers can update description, social links, image, active status
- `DeleteTeamAsync` — removes managers, media records, and directory
- `GetTeamManagerViewModelAsync` / `AddTeamManagerAsync` / `RemoveTeamManagerAsync` — manager assignment (Admin only)
- `GetAllUserManagedTeams` — teams the current user manages
- `GetTeamMediaViewModelAsync` / `UploadTeamMediaAsync` / `DeleteTeamMediaAsync` — media management
- Access check: `HasTeamAccess(team)` — true if Admin or listed in `team.Managers`

### `GameService` (`IGameService`)
- `GetPaginatedGamesAsync` — paginated/searchable game list
- `PrepareGameCreateViewModelViewModel` — builds select lists for host/winner dropdowns
- `CreateGameAsync` — validates uniqueness of Year/Order, creates game + GameDays for all GameDayTypes + `wwwroot/media/games/{year}/` directory
- `GetGameBySlugAsync(slug, includeFullDetails)` — access-checked; loads full nav when `includeFullDetails = true`
- `GetGameDaysViewModelAsync(slug)` — access-checked
- `UpdateGameDaysAsync` — toggles `IsEnabled` per day
- `PrepareGameUpdateViewModelAsync(slug, viewModel?)` — access-checked; builds select lists for admin only
- `UpdateGameAsync(viewModel, image, modelState)` — admin can update Year/Order/HostId/WinnerId/Slug; all roles can update Description + Image
- Access check: `HasGameAccess(game)` — true if Admin or listed in `game.Host.Managers`; `CanUserAccessGameAsync(slug)` — async version that loads the game+host+managers

### `DirectoryService` (`IDirectoryService`)
- `CreateDirectory(path)` — creates under `wwwroot/media/`
- `DeleteDirectory(path)` — recursive delete
- `SaveFileAsync(file, storagePath, desiredFileName?)` — saves file; if `desiredFileName` provided, deletes existing files with same prefix first; returns web-relative path `media/...`
- `DeleteFile(path)` — deletes single file
- Max file size: 100 MB. Generated filenames: 11-char alphanumeric short ID.

### `EmailService` (`IEmailService`)
- `SendAccountCreationEmailAsync` — sends temp password to new user
- `SendPasswordResetEmailAsync` — sends reset link with token

## Controllers

Controllers are thin. They call a service method, check `serviceResponse.Success`, set a TempData message via `AlertMessageHandler.SetMessage`, and redirect or return a view. They never resolve user identity to pass into services.

### `HomeController`
- `GET /` → `Index`
- `GET /error` → `Error` (sets 500)
- `GET /not-found` → `NotFound` (sets 404)
- `GET /admin-dashboard` → `AdminDashboard` [Admin]

### `AuthenticationController`
- `GET/POST /account/login` → `Login`
- `POST /account/logout` → `Logout`
- `GET/POST /account/forgot-password` → `ForgotPassword`
- `GET/POST /account/reset-password` → `ResetPassword`

### `AccountController`
- `GET /account/manage` → `AccountManagement` [Admin]
- `GET/POST /account/create` → `AccountCreate` [Admin]
- `GET /account/settings` → `AccountSettings` [Auth]
- `POST /account/change-email` → `ChangeEmail` [Auth]
- `POST /account/change-password` → `ChangePassword` [Auth]

### `TeamController` (`/teams`)
- `GET /teams` → `TeamList` (public)
- `GET /teams/manage` → `TeamManagement` [Admin]
- `GET/POST /teams/create` → `TeamCreate` [Admin]
- `GET /teams/{slug}` → `TeamDetails` (public, full details)
- `GET /teams/{slug}/actions` → `TeamActions` [Admin, Manager] — loads `HostedGames` always; managers see hosted games section in view
- `GET/POST /teams/{slug}/update` → `TeamUpdate` [Admin, Manager]
- `POST /teams/{slug}/delete` → `DeleteTeam` [Admin]
- `GET /teams/{slug}/managers` → `TeamManagers` [Admin]
- `POST /teams/{slug}/managers/add` → `AddTeamManager` [Admin]
- `POST /teams/{slug}/managers/remove` → `RemoveTeamManager` [Admin]
- `GET /teams/{slug}/media` → `TeamMedia` [Admin, Manager]
- `POST /teams/{slug}/media/upload` → `UploadTeamMedia` [Admin, Manager]
- `POST /teams/{slug}/media/delete/{id}` → `DeleteTeamMedia` [Admin, Manager]
- `GET /teams/manager-dashboard` → `TeamManagerDashboard` [Manager]

### `GameController` (`/games`)
- `GET /games` → `GameList` (public)
- `GET /games/manage` → `GameManagement` [Admin]
- `GET/POST /games/create` → `GameCreate` [Admin]
- `GET /games/{slug}/actions` → `GameActions` [Admin, Manager]
- `GET/POST /games/{slug}/update` → `GameUpdate` [Admin, Manager] — admin: slug/year/order/host/winner + description/image; manager: description/image only
- `GET/POST /games/{slug}/days` → `GameDays` / `UpdateGameDays` [Admin, Manager]

## UI Patterns

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
ServiceResponse.Warning("message")
ServiceResponse<T>.Successful(data, "message")
ServiceResponse<T>.Failure("message")
```

Controllers check `serviceResponse.Success`, call `AlertMessageHandler.SetMessage(TempData, serviceResponse)`, then redirect or return a view with `serviceResponse.Data`.
