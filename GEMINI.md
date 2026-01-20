# KunigiArchive Project Context

## Project Overview
KunigiArchive is an ASP.NET Core MVC web application designed to archive and manage the history of "Kunigi" games. It serves as a central repository for Teams, Games, Puzzles, and associated multimedia content. The application is built on the future **.NET 10** framework.

## Tech Stack
*   **Framework:** .NET 10.0 (`net10.0`)
*   **Type:** ASP.NET Core MVC
*   **Database:** SQLite (`Microsoft.EntityFrameworkCore.Sqlite`)
*   **ORM:** Entity Framework Core
*   **Authentication:** ASP.NET Core Identity
*   **Logging:** Serilog (Console and File sinks)
*   **Frontend:** Razor Views, Bootstrap, jQuery

## Building and Running

### Prerequisites
*   .NET 10.0 SDK

### Key Commands
```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application (from root)
dotnet run --project KunigiArchive.Web

# Run with hot reload (Recommended for development)
dotnet watch run --project KunigiArchive.Web
```

### Database Management
The application applies pending migrations and seeds data automatically on startup (`Program.cs`).
To create a new migration after modifying Entities:
```bash
dotnet ef migrations add <MigrationName> --project KunigiArchive.Web --output-dir Data/Migrations
```

## Architecture & patterns

### Directory Structure
*   **`Controllers/`**: Handle HTTP requests. Most logic is delegated to Services.
*   **`Services/`**: Business logic layer. All services are registered as `Scoped`.
    *   `AlertMessageHandler`: Helper for passing success/error messages to Views via `TempData`.
*   **`Entities/`**: Database models (Code-First EF Core).
*   **`ViewModels/`**: DTOs for View rendering and Form submission.
*   **`Data/`**: `DataContext` configuration and Seeding logic.
*   **`wwwroot/media/`**: Storage location for user-uploaded content (Teams/Games images).

### Key Conventions
*   **Routing:** Entities often use a `Slug` property for friendly URLs (e.g., `/teams/some-team-name`) instead of IDs.
*   **Media Handling:** Files are uploaded to the local file system (`wwwroot/media`). `DirectoryService` manages path resolution.
*   **Authorization:**
    *   Role-based access control is strictly enforced (`[Authorize(Roles = "Admin")]`).
    *   Main roles: `Admin`, `Manager`.
    *   `Manager` role is usually scoped to specific Teams.
*   **Error Handling:** Custom exception pages (`/error`) in production; Developer page in development.
*   **Logging:** Structured logging via Serilog is configured in `appsettings.json` to write to `Logs/`.

## Configuration
*   **`appsettings.json`**: Contains DB connection strings (`DefaultConnection`), Email settings (`Mailtrap`), and Serilog config.
*   **`Program.cs`**: Registers services, configures the HTTP pipeline, and handles database migration/seeding on startup.
