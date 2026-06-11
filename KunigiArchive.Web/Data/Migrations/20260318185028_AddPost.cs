#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace KunigiArchive.Web.Data.Migrations;

/// <inheritdoc />
public partial class AddPost : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "Posts",
            table => new
            {
                PostId = table.Column<long>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                GameDayId = table.Column<long>("INTEGER", nullable: false),
                Content = table.Column<string>("TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>("TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Posts", x => x.PostId);
                table.ForeignKey(
                    "FK_Posts_GameDays_GameDayId",
                    x => x.GameDayId,
                    "GameDays",
                    "GameDayId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            "IX_Posts_GameDayId",
            "Posts",
            "GameDayId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "Posts");
    }
}