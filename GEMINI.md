# KunigiArchive

## Project Overview

**KunigiArchive** is an ASP.NET Core MVC web application designed to serve as a digital archive for "To Kinigi" (The Hunt), likely a scavenger hunt or team-based competition. It manages historical data about games, teams, puzzles, and associated media.

### Technical Stack

*   **Framework:** .NET 10.0 (ASP.NET Core MVC)
*   **Database:** SQLite
*   **ORM:** Entity Framework Core 10.0.1
*   **Authentication/Authorization:** ASP.NET Core Identity
*   **Logging:** Serilog (Console & Rolling File sinks)
*   **Frontend:** Razor Views (Server-side rendering) with Bootstrap/jQuery (inferred from `wwwroot/lib`)
*   **Email:** Mailtrap (configured in `appsettings.json`)

## Architecture & Directory Structure

The project follows a standard monolithic N-Tier architecture organized by function:

*   **`KunigiArchive.Web/`**: The main web application project.
    *   **`Controllers/`**: Handles HTTP requests and application flow (`GameController`, `TeamController`, etc.).
    *   **`Data/`**: Database context (`DataContext`), migrations, and seeding logic (`DataSeed`).
    *   **`Entities/`**: Domain models representing the database schema (`Game`, `Team`, `Puzzle`, `AppUser`).
    *   **`Services/`**: Business logic layer (`GameService`, `TeamService`, `EmailService`) interacting with repositories/context.
    *   **`ViewModels/`**: DTOs for carrying data between Controllers and Views.
    *   **`Views/`**: Razor pages for the UI.
    *   **`wwwroot/`**: Static files (CSS, JS, images). User-uploaded media is stored in `wwwroot/media`.

## Building and Running

### Prerequisites

*   .NET 10.0 SDK

### Commands

**Restore Dependencies:**
```bash
dotnet restore
```

**Build:**
```bash
dotnet build
```

**Run Application:**
```bash
cd KunigiArchive.Web
dotnet run
```
*The application will automatically apply pending migrations and seed default data (Roles, Admin User) on startup.*

## Configuration & Setup

### Database
The application uses SQLite. The connection string is defined in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=kunigi_archive.db"
}
```
The database file `kunigi_archive.db` will be created in the running directory if it does not exist.

### Authentication & Seeding
On startup, `DataSeed.Seed` ensures the following:
*   **Roles:** "Admin", "Manager"
*   **Admin User:** `iperpirakis@gmail.com` (Password: `Admin@123!`)
*   **Development User:** `manager@test.com` (Password: `Manager@123!`) - *Only in Development environment*
*   **GameDayTypes:** Seeds initial types like "Χωρός", "Σάββατο", etc.

### Logging
Logs are written to:
1.  Console
2.  File: `Logs/app-{Date}.log`

## Development Conventions

*   **Namespace:** `KunigiArchive.Web`
*   **Styling:** Follows standard C# conventions.
*   **Dependency Injection:** Services are registered in `Program.cs`. Most business services are scoped (`AddScoped`).
*   **Static Assets:** Media files uploaded by users are stored in `wwwroot/media`, which is created at runtime if missing.
