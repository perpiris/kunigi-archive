#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace KunigiArchive.Web.Data.Mirations;

/// <inheritdoc />
public partial class init : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "AspNetRoles",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>("TEXT", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>("TEXT", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>("TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            "AspNetUsers",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserName = table.Column<string>("TEXT", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>("TEXT", maxLength: 256, nullable: true),
                Email = table.Column<string>("TEXT", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>("TEXT", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>("INTEGER", nullable: false),
                PasswordHash = table.Column<string>("TEXT", nullable: true),
                SecurityStamp = table.Column<string>("TEXT", nullable: true),
                ConcurrencyStamp = table.Column<string>("TEXT", nullable: true),
                PhoneNumber = table.Column<string>("TEXT", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>("INTEGER", nullable: false),
                TwoFactorEnabled = table.Column<bool>("INTEGER", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>("TEXT", nullable: true),
                LockoutEnabled = table.Column<bool>("INTEGER", nullable: false),
                AccessFailedCount = table.Column<int>("INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUsers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            "GameDayTypes",
            table => new
            {
                GameDayTypeId = table.Column<long>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Label = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                Slug = table.Column<string>("TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GameDayTypes", x => x.GameDayTypeId);
            });

        migrationBuilder.CreateTable(
            "Teams",
            table => new
            {
                TeamId = table.Column<long>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>("TEXT", maxLength: 255, nullable: false),
                Slug = table.Column<string>("TEXT", maxLength: 255, nullable: false),
                IsActive = table.Column<bool>("INTEGER", nullable: false, defaultValue: true),
                YearFounded = table.Column<short>("INTEGER", nullable: true),
                Description = table.Column<string>("TEXT", nullable: true),
                Facebook = table.Column<string>("TEXT", maxLength: 255, nullable: true),
                Instagram = table.Column<string>("TEXT", maxLength: 255, nullable: true),
                Youtube = table.Column<string>("TEXT", maxLength: 255, nullable: true),
                Website = table.Column<string>("TEXT", maxLength: 255, nullable: true),
                Image = table.Column<string>("TEXT", maxLength: 255, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Teams", x => x.TeamId);
            });

        migrationBuilder.CreateTable(
            "AspNetRoleClaims",
            table => new
            {
                Id = table.Column<int>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RoleId = table.Column<long>("INTEGER", nullable: false),
                ClaimType = table.Column<string>("TEXT", nullable: true),
                ClaimValue = table.Column<string>("TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                table.ForeignKey(
                    "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                    x => x.RoleId,
                    "AspNetRoles",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "AspNetUserClaims",
            table => new
            {
                Id = table.Column<int>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<long>("INTEGER", nullable: false),
                ClaimType = table.Column<string>("TEXT", nullable: true),
                ClaimValue = table.Column<string>("TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey(
                    "FK_AspNetUserClaims_AspNetUsers_UserId",
                    x => x.UserId,
                    "AspNetUsers",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "AspNetUserLogins",
            table => new
            {
                LoginProvider = table.Column<string>("TEXT", nullable: false),
                ProviderKey = table.Column<string>("TEXT", nullable: false),
                ProviderDisplayName = table.Column<string>("TEXT", nullable: true),
                UserId = table.Column<long>("INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    "FK_AspNetUserLogins_AspNetUsers_UserId",
                    x => x.UserId,
                    "AspNetUsers",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "AspNetUserRoles",
            table => new
            {
                UserId = table.Column<long>("INTEGER", nullable: false),
                RoleId = table.Column<long>("INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    "FK_AspNetUserRoles_AspNetRoles_RoleId",
                    x => x.RoleId,
                    "AspNetRoles",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_AspNetUserRoles_AspNetUsers_UserId",
                    x => x.UserId,
                    "AspNetUsers",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "AspNetUserTokens",
            table => new
            {
                UserId = table.Column<long>("INTEGER", nullable: false),
                LoginProvider = table.Column<string>("TEXT", nullable: false),
                Name = table.Column<string>("TEXT", nullable: false),
                Value = table.Column<string>("TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    "FK_AspNetUserTokens_AspNetUsers_UserId",
                    x => x.UserId,
                    "AspNetUsers",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "Games",
            table => new
            {
                GameId = table.Column<long>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Year = table.Column<short>("INTEGER", nullable: false),
                Order = table.Column<short>("INTEGER", nullable: false),
                Slug = table.Column<string>("TEXT", maxLength: 255, nullable: false),
                OrderTitle = table.Column<string>("TEXT", maxLength: 255, nullable: false),
                Title = table.Column<string>("TEXT", maxLength: 255, nullable: true),
                Description = table.Column<string>("TEXT", nullable: true),
                Image = table.Column<string>("TEXT", maxLength: 255, nullable: true),
                HostId = table.Column<long>("INTEGER", nullable: false),
                WinnerId = table.Column<long>("INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Games", x => x.GameId);
                table.ForeignKey(
                    "FK_Games_Teams_HostId",
                    x => x.HostId,
                    "Teams",
                    "TeamId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Games_Teams_WinnerId",
                    x => x.WinnerId,
                    "Teams",
                    "TeamId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "TeamManagers",
            table => new
            {
                AppUserId = table.Column<long>("INTEGER", nullable: false),
                TeamId = table.Column<long>("INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TeamManagers", x => new { x.TeamId, x.AppUserId });
                table.ForeignKey(
                    "FK_TeamManagers_AspNetUsers_AppUserId",
                    x => x.AppUserId,
                    "AspNetUsers",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_TeamManagers_Teams_TeamId",
                    x => x.TeamId,
                    "Teams",
                    "TeamId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "TeamMedia",
            table => new
            {
                TeamMediaId = table.Column<long>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TeamId = table.Column<long>("INTEGER", nullable: false),
                Path = table.Column<string>("TEXT", maxLength: 500, nullable: false),
                OriginalFileName = table.Column<string>("TEXT", maxLength: 255, nullable: false),
                FileName = table.Column<string>("TEXT", maxLength: 255, nullable: false),
                TimeStamp = table.Column<DateTime>("TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TeamMedia", x => x.TeamMediaId);
                table.ForeignKey(
                    "FK_TeamMedia_Teams_TeamId",
                    x => x.TeamId,
                    "Teams",
                    "TeamId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "GameDays",
            table => new
            {
                GameDayId = table.Column<long>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Title = table.Column<string>("TEXT", maxLength: 255, nullable: true),
                Description = table.Column<string>("TEXT", nullable: true),
                IsEnabled = table.Column<bool>("INTEGER", nullable: false, defaultValue: false),
                GameTypeId = table.Column<long>("INTEGER", nullable: false),
                GameId = table.Column<long>("INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GameDays", x => x.GameDayId);
                table.ForeignKey(
                    "FK_GameDays_GameDayTypes_GameTypeId",
                    x => x.GameTypeId,
                    "GameDayTypes",
                    "GameDayTypeId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_GameDays_Games_GameId",
                    x => x.GameId,
                    "Games",
                    "GameId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "GameMedia",
            table => new
            {
                GameMediaId = table.Column<long>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                GameId = table.Column<long>("INTEGER", nullable: false),
                Path = table.Column<string>("TEXT", maxLength: 500, nullable: false),
                OriginalFileName = table.Column<string>("TEXT", maxLength: 255, nullable: false),
                FileName = table.Column<string>("TEXT", maxLength: 255, nullable: false),
                TimeStamp = table.Column<DateTime>("TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GameMedia", x => x.GameMediaId);
                table.ForeignKey(
                    "FK_GameMedia_Games_GameId",
                    x => x.GameId,
                    "Games",
                    "GameId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "GameDayMedia",
            table => new
            {
                GameDayMediaId = table.Column<long>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                GameDayId = table.Column<long>("INTEGER", nullable: false),
                Path = table.Column<string>("TEXT", maxLength: 500, nullable: false),
                OriginalFileName = table.Column<string>("TEXT", maxLength: 255, nullable: false),
                FileName = table.Column<string>("TEXT", maxLength: 255, nullable: false),
                TimeStamp = table.Column<DateTime>("TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GameDayMedia", x => x.GameDayMediaId);
                table.ForeignKey(
                    "FK_GameDayMedia_GameDays_GameDayId",
                    x => x.GameDayId,
                    "GameDays",
                    "GameDayId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "Puzzles",
            table => new
            {
                PuzzleId = table.Column<long>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                GameDayId = table.Column<long>("INTEGER", nullable: false),
                Order = table.Column<short>("INTEGER", nullable: false),
                Group = table.Column<short>("INTEGER", nullable: true),
                Question = table.Column<string>("TEXT", nullable: true),
                Answer = table.Column<string>("TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Puzzles", x => x.PuzzleId);
                table.ForeignKey(
                    "FK_Puzzles_GameDays_GameDayId",
                    x => x.GameDayId,
                    "GameDays",
                    "GameDayId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "PuzzleMedia",
            table => new
            {
                PuzzleMediaId = table.Column<long>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PuzzleId = table.Column<long>("INTEGER", nullable: false),
                MediaType = table.Column<string>("TEXT", maxLength: 50, nullable: false),
                Path = table.Column<string>("TEXT", maxLength: 500, nullable: false),
                OriginalFileName = table.Column<string>("TEXT", maxLength: 255, nullable: false),
                FileName = table.Column<string>("TEXT", maxLength: 255, nullable: false),
                TimeStamp = table.Column<DateTime>("TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PuzzleMedia", x => x.PuzzleMediaId);
                table.ForeignKey(
                    "FK_PuzzleMedia_Puzzles_PuzzleId",
                    x => x.PuzzleId,
                    "Puzzles",
                    "PuzzleId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            "IX_AspNetRoleClaims_RoleId",
            "AspNetRoleClaims",
            "RoleId");

        migrationBuilder.CreateIndex(
            "RoleNameIndex",
            "AspNetRoles",
            "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_AspNetUserClaims_UserId",
            "AspNetUserClaims",
            "UserId");

        migrationBuilder.CreateIndex(
            "IX_AspNetUserLogins_UserId",
            "AspNetUserLogins",
            "UserId");

        migrationBuilder.CreateIndex(
            "IX_AspNetUserRoles_RoleId",
            "AspNetUserRoles",
            "RoleId");

        migrationBuilder.CreateIndex(
            "EmailIndex",
            "AspNetUsers",
            "NormalizedEmail");

        migrationBuilder.CreateIndex(
            "UserNameIndex",
            "AspNetUsers",
            "NormalizedUserName",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_GameDayMedia_GameDayId",
            "GameDayMedia",
            "GameDayId");

        migrationBuilder.CreateIndex(
            "IX_GameDays_GameId",
            "GameDays",
            "GameId");

        migrationBuilder.CreateIndex(
            "IX_GameDays_GameTypeId",
            "GameDays",
            "GameTypeId");

        migrationBuilder.CreateIndex(
            "IX_GameMedia_GameId",
            "GameMedia",
            "GameId");

        migrationBuilder.CreateIndex(
            "IX_Games_HostId",
            "Games",
            "HostId");

        migrationBuilder.CreateIndex(
            "IX_Games_Slug",
            "Games",
            "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_Games_WinnerId",
            "Games",
            "WinnerId");

        migrationBuilder.CreateIndex(
            "IX_PuzzleMedia_PuzzleId",
            "PuzzleMedia",
            "PuzzleId");

        migrationBuilder.CreateIndex(
            "IX_Puzzles_GameDayId",
            "Puzzles",
            "GameDayId");

        migrationBuilder.CreateIndex(
            "IX_TeamManagers_AppUserId",
            "TeamManagers",
            "AppUserId");

        migrationBuilder.CreateIndex(
            "IX_TeamMedia_TeamId",
            "TeamMedia",
            "TeamId");

        migrationBuilder.CreateIndex(
            "IX_Teams_Slug",
            "Teams",
            "Slug",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "AspNetRoleClaims");

        migrationBuilder.DropTable(
            "AspNetUserClaims");

        migrationBuilder.DropTable(
            "AspNetUserLogins");

        migrationBuilder.DropTable(
            "AspNetUserRoles");

        migrationBuilder.DropTable(
            "AspNetUserTokens");

        migrationBuilder.DropTable(
            "GameDayMedia");

        migrationBuilder.DropTable(
            "GameMedia");

        migrationBuilder.DropTable(
            "PuzzleMedia");

        migrationBuilder.DropTable(
            "TeamManagers");

        migrationBuilder.DropTable(
            "TeamMedia");

        migrationBuilder.DropTable(
            "AspNetRoles");

        migrationBuilder.DropTable(
            "Puzzles");

        migrationBuilder.DropTable(
            "AspNetUsers");

        migrationBuilder.DropTable(
            "GameDays");

        migrationBuilder.DropTable(
            "GameDayTypes");

        migrationBuilder.DropTable(
            "Games");

        migrationBuilder.DropTable(
            "Teams");
    }
}