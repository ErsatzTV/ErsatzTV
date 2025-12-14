using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Fix_SmartCollectionNameSensitivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
    WITH Numbered AS (
        SELECT
            Id,
            -- Partition by case-insensitive name
            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
        FROM SmartCollection
    )
    UPDATE SmartCollection
    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
    FROM Numbered
    WHERE SmartCollection.Id = Numbered.Id
      AND Numbered.RowNum > 1;
");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SmartCollection",
                type: "TEXT",
                nullable: true,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SmartCollection",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true,
                oldCollation: "NOCASE");
        }
    }
}
