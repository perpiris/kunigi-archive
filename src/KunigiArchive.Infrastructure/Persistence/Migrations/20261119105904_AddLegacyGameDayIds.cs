using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KunigiArchive.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Keeps the old MVC site's day ids for its links (/games/{slug}/day/{id}, #91) in LegacyGameDayIds, copied from
    /// the import's id map, then drops the import's own table: the Import project is gone (#89). On a database the
    /// import never ran on (development) the table is simply empty. Not part of the EF model; read by LegacyDayLinks.
    /// </summary>
    public partial class AddLegacyGameDayIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE dbo.LegacyGameDayIds (
                    OldId bigint NOT NULL CONSTRAINT PK_LegacyGameDayIds PRIMARY KEY,
                    GameDayId uniqueidentifier NOT NULL
                        CONSTRAINT FK_LegacyGameDayIds_GameDays REFERENCES dbo.GameDays (Id) ON DELETE CASCADE);
                """);

            // Dynamic SQL: import.IdMap only exists where the import ran.
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'import.IdMap') IS NOT NULL
                BEGIN
                    EXEC (N'INSERT INTO dbo.LegacyGameDayIds (OldId, GameDayId)
                            SELECT m.OldId, m.NewId FROM import.IdMap m
                            JOIN dbo.GameDays d ON d.Id = m.NewId
                            WHERE m.TableName = N''GameDays'';');
                    DROP TABLE import.IdMap;
                END
                IF SCHEMA_ID(N'import') IS NOT NULL
                    EXEC (N'DROP SCHEMA import;');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // import.IdMap isn't recreated: the Import project that used it is gone.
            migrationBuilder.Sql("DROP TABLE dbo.LegacyGameDayIds;");
        }
    }
}
