using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KunigiArchive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GameOrderOptionalHostAndNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_Order",
                table: "Games");

            migrationBuilder.AlterColumn<decimal>(
                name: "Order",
                table: "Games",
                type: "decimal(4,1)",
                precision: 4,
                scale: 1,
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<Guid>(
                name: "HostId",
                table: "Games",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Games",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_Order",
                table: "Games",
                column: "Order",
                unique: true,
                filter: "[Order] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_Order",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Games");

            migrationBuilder.AlterColumn<short>(
                name: "Order",
                table: "Games",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,1)",
                oldPrecision: 4,
                oldScale: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "HostId",
                table: "Games",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_Order",
                table: "Games",
                column: "Order",
                unique: true);
        }
    }
}
