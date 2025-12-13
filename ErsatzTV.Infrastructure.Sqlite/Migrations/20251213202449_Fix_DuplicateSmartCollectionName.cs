using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Fix_DuplicateSmartCollectionName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
    WITH Numbered AS (
        SELECT
            Id,
            ROW_NUMBER() OVER (PARTITION BY Name ORDER BY Id) AS RowNum
        FROM SmartCollection
    )
    UPDATE SmartCollection
    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
    FROM Numbered
    WHERE SmartCollection.Id = Numbered.Id
      AND Numbered.RowNum > 1;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
