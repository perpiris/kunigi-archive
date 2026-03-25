using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KunigiArchive.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MovePostsToGameAddGameDayImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_GameDays_GameDayId",
                table: "Posts");

            migrationBuilder.RenameColumn(
                name: "GameDayId",
                table: "Posts",
                newName: "GameId");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_GameDayId",
                table: "Posts",
                newName: "IX_Posts_GameId");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "GameDays",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Games_GameId",
                table: "Posts",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "GameId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Games_GameId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "GameDays");

            migrationBuilder.RenameColumn(
                name: "GameId",
                table: "Posts",
                newName: "GameDayId");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_GameId",
                table: "Posts",
                newName: "IX_Posts_GameDayId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_GameDays_GameDayId",
                table: "Posts",
                column: "GameDayId",
                principalTable: "GameDays",
                principalColumn: "GameDayId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
