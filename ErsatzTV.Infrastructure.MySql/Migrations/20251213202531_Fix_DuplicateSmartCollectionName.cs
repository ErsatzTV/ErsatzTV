using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
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
            ROW_NUMBER() OVER (PARTITION BY Name ORDER BY Id) as RowNum
        FROM SmartCollection
    )
    UPDATE SmartCollection sc
    JOIN Numbered n ON sc.Id = n.Id
    SET sc.Name = CONCAT(sc.Name, ' (', n.RowNum - 1, ')')
    WHERE n.RowNum > 1;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
