using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Fix_DuplicateNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE Block a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM Block
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE BlockGroup a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM BlockGroup
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE ChannelWatermark a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM ChannelWatermark
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE Collection a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM Collection
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE Deco a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM Deco
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE DecoGroup a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM DecoGroup
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE DecoTemplate a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM DecoTemplate
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE DecoTemplateGroup a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM DecoTemplateGroup
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE FillerPreset a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM FillerPreset
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE MultiCollection a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM MultiCollection
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE Playlist a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM Playlist
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE ProgramSchedule a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM ProgramSchedule
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE RerunCollection a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM RerunCollection
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE SmartCollection a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM SmartCollection
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE Template a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM Template
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                UPDATE TemplateGroup a
                    JOIN (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (PARTITION BY LOWER(Name) ORDER BY Id) as RowNum
                        FROM TemplateGroup
                    ) n ON a.Id = n.Id
                SET a.Name = CONCAT(a.Name, ' (', n.RowNum - 1, ')')
                WHERE n.RowNum > 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
