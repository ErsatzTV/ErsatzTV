using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Fix_SmartCollectionNameSensitivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
UPDATE SmartCollection sc
    JOIN (
        SELECT
            Id,
            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
        FROM SmartCollection
    ) n ON sc.Id = n.Id
SET sc.Name = CONCAT(sc.Name, ' (', n.RowNum - 1, ')')
WHERE n.RowNum > 1;
");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SmartCollection",
                type: "varchar(255)",
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SmartCollection",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true,
                oldCollation: "utf8mb4_general_ci")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
