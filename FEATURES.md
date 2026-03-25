# Feature Status

Comparison between the original [Kunigi](https://github.com/perpiris/kunigi) repository and KunigiArchive.

## Missing Features (from original, not yet implemented)

Entities and EF configurations exist for all of these, but there is no service, controller, view model, or view code.

### Puzzle CRUD

- Create, edit, and delete puzzles within game days
- Puzzles have order, group, question text, and answer text
- Auto-incrementing order; deleting a puzzle re-orders remaining ones and dissolves single-puzzle groups
- **Original location:** `GameService.cs` (PrepareCreatePuzzleViewModel, CreatePuzzle, PrepareEditPuzzleViewModel, EditPuzzle, DeletePuzzle), `GameController.cs` (CreatePuzzle, EditPuzzle, DeletePuzzle, ManagePuzzles), views under `/Views/Game/`

### Puzzle Media Management

- Upload question media (type "Q") and answer media (type "A") per puzzle
- Dedicated media management view per puzzle with delete capability
- `PuzzleMedia` entity exists with `MediaType` discriminator field
- **Original location:** `GameService.cs` (GetPuzzleMedia, DeletePuzzleMedia), `GameController.cs` (PuzzleMediaManagement, DeletePuzzleMedia), `/Views/Game/PuzzleMediaManagement.cshtml`

### Public Puzzle Timeline Display

- Public-facing view showing all puzzles for a game day in a timeline layout
- Puzzles can be grouped into packages; grouped puzzles display together in a single timeline node
- Bootstrap-based timeline with alternating left/right items, showing question/answer with media
- `GameDayDetails.cshtml` currently shows a "Coming Soon" placeholder
- **Original location:** `GameController.cs` (PuzzleList), `/Views/Game/PuzzleList.cshtml`, `/Views/Shared/_PuzzleContent.cshtml`

### Game Media Management

- Upload and delete media files for a game
- Separate media management page accessible from game actions
- `GameMedia` entity exists with full schema
- **Original location:** `GameService.cs` (GetParentGameMedia, AddParentGameMedia, DeleteParentGameMedia), `GameController.cs` (ParentGameMediaManagement, UploadParentGameMedia, DeleteParentGameMedia), `/Views/Game/ParentGameMediaManagement.cshtml`

### GameDay Editing (Title and Description)

- Edit title and description per individual game day via a dedicated form
- `GameDay` entity has `Title` and `Description` fields; current UI only toggles `IsEnabled`
- **Original location:** `GameService.cs` (PrepareGameEditViewModel, EditGame), `GameController.cs` (EditGame), `/Views/Game/EditGame.cshtml`

### Role Editing

- Admin can view a user's current roles and add/remove roles via checkbox form
- Currently no way to change a user's role after account creation
- **Original location:** `AccountController.cs` (EditRoles), `/Views/Account/EditRoles.cshtml`

## New Features (KunigiArchive only, not in original)

### Implemented

- **Slug-based routing** — All entities use slugs in URLs instead of GUIDs
- **Password reset flow** — Token-based reset via memory cache (1h expiry)
- **Email service** — Sends account creation and password reset emails
- **Search/filter on lists** — Paginated lists support `searchQuery` parameter
- **Team deletion** — Full cascade delete with directory cleanup
- **Account self-service** — Users can change their own email and password
- **Admin-created accounts** — Replaces public self-registration; admin creates account with role, temp password emailed

### Not Yet Implemented

- **Post CRUD** — `Post` entity exists (linked to `GameDay`), no service/controller/views
- **GameDay Media Management** — `GameDayMedia` entity exists, no service/controller/views

## Deliberate Design Changes

| Area | Original Kunigi | KunigiArchive |
|------|----------------|---------------|
| Identity keys | `string` (GUID) | `long` |
| Database | SQL Server | SQLite |
| Error handling | Custom exceptions + global filter | `ServiceResponse<T>` pattern |
| Routing | GUID-based | Slug-based |
| Entity IDs | GUIDs | Long integers |
| Registration | Public self-registration | Admin-initiated with emailed temp password |
| GameDay types | Selectable at game creation | Auto-create all types, toggle `IsEnabled` |
| Media storage | Shared `MediaFile` entity + join tables | Denormalized per-entity media tables |
| Game hierarchy | `ParentGame` → `Game` | `Game` → `GameDay` (renamed) |
| Logging | Default | Serilog |
| Framework | .NET 8 | .NET 10 |

## Completed Features

- Team CRUD (create, read, update, delete)
- Team media management (upload, delete, gallery)
- Team manager assignment (add, remove)
- Manager dashboard (view managed teams)
- Game CRUD (create, read, update)
- Game day toggle (enable/disable)
- Account management (paginated list, create)
- Authentication (login, logout, forgot/reset password)
- Admin dashboard
- Public team list and details
- Public game list
