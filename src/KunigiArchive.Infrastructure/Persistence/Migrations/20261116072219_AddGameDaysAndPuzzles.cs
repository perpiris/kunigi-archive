using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KunigiArchive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameDaysAndPuzzles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameDays_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameDayMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameDayMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameDayMedia_GameDays_GameDayId",
                        column: x => x.GameDayId,
                        principalTable: "GameDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Puzzles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Group = table.Column<int>(type: "int", nullable: true),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Puzzles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Puzzles_GameDays_GameDayId",
                        column: x => x.GameDayId,
                        principalTable: "GameDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PuzzleMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PuzzleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuzzleMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuzzleMedia_Puzzles_PuzzleId",
                        column: x => x.PuzzleId,
                        principalTable: "Puzzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameDayMedia_GameDayId",
                table: "GameDayMedia",
                column: "GameDayId");

            migrationBuilder.CreateIndex(
                name: "IX_GameDays_GameId_Type",
                table: "GameDays",
                columns: new[] { "GameId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PuzzleMedia_PuzzleId",
                table: "PuzzleMedia",
                column: "PuzzleId");

            migrationBuilder.CreateIndex(
                name: "IX_Puzzles_GameDayId_Order",
                table: "Puzzles",
                columns: new[] { "GameDayId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameDayMedia");

            migrationBuilder.DropTable(
                name: "PuzzleMedia");

            migrationBuilder.DropTable(
                name: "Puzzles");

            migrationBuilder.DropTable(
                name: "GameDays");
        }
    }
}
