# KunigiArchive

KunigiArchive is a web-based management and archive system designed to organize and present the history of Kunigi games. It serves as a central hub for storing information about teams, games, puzzles, and multimedia content associated with these events.

## Application Structure

The project is built as an ASP.NET Core Web application, following a service-oriented architecture:

*   **Controllers:** Handle HTTP requests, authentication, and routing. They act as the entry point for user actions and delegate business logic to services.
*   **Services:** Contain the core business logic and data manipulation rules. This layer handles validation, data processing, and interactions with the database.
*   **Entities:** Define the core domain models (database tables) representing the real-world objects like Games, Teams, and Puzzles.
*   **ViewModels:** Specialized data transfer objects used to pass data between the controllers and the views, ensuring that only necessary data is exposed to the UI.
*   **Views:** Razor pages that render the user interface.

## Core Domain Entities

### 1. Teams
Teams are the central participants in the Kunigi ecosystem.
*   **Profile:** Each team has a profile with a name, history (year founded), description, and social media links.
*   **Roles:** A team can be a **Host** (organizer of a game) or a **Winner** of a specific game.
*   **Management:** Teams can be managed by system Administrators or assigned "Team Managers".

### 2. Games
Represents a specific event or edition of the game (e.g., "Kunigi 2024").
*   **Properties:** Includes a title, year, description, and an identifying slug.
*   **Associations:** Each game is linked to a Host Team and a Winner Team.
*   **Structure:** A Game is divided into **Game Days**, which in turn contain **Puzzles**.
*   **Content:** Games function as an archive, storing media and historical data about the event.

### 3. Puzzles
The challenges presented during a Game Day.
*   **Details:** Contains the question/riddle, the correct answer, and associated media (images/files).

## Key Workflows & Features

### Team Operations
*   **Creation & Management (Admin):** Administrators can create new teams, edit their core details, and delete them if necessary.
*   **Team Managers:** Administrators can assign specific users as "Managers" for a team. This allows those users to edit their specific team's details without having full system admin privileges.
*   **Media Gallery:** Teams can have a dedicated media gallery. Managers and Admins can upload and remove photos/videos related to the team.

### Game Operations
*   **Game Archiving:** Administrators can create new game entries, specifying the year, order, and order title.
*   **Host/Winner Assignment:** Linking specific teams to games as hosts or winners to build the historical record.
*   **Day & Puzzle Management:** (In Progress) detailed management of game days and the specific puzzles for each day.

### User Roles & Permissions
*   **Admin:** Full access to the system. Can create/delete any entity (Teams, Games, Users) and assign roles.
*   **Manager:** Restricted administrative access. Can only edit and manage the specific Teams they have been assigned to (e.g., updating the team description, uploading media).
*   **Public/User:** Read-only access to the public archive. Can view game histories, team profiles, and public galleries.
