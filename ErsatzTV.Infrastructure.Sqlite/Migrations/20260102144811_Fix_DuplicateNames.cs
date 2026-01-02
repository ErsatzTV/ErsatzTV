using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Fix_DuplicateNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM Block
                    )
                    UPDATE Block
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE Block.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM BlockGroup
                    )
                    UPDATE BlockGroup
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE BlockGroup.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM ChannelWatermark
                    )
                    UPDATE ChannelWatermark
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE ChannelWatermark.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM Collection
                    )
                    UPDATE Collection
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE Collection.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM Deco
                    )
                    UPDATE Deco
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE Deco.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM DecoGroup
                    )
                    UPDATE DecoGroup
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE DecoGroup.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM DecoTemplate
                    )
                    UPDATE DecoTemplate
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE DecoTemplate.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM DecoTemplateGroup
                    )
                    UPDATE DecoTemplateGroup
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE DecoTemplateGroup.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM FillerPreset
                    )
                    UPDATE FillerPreset
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE FillerPreset.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM MultiCollection
                    )
                    UPDATE MultiCollection
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE MultiCollection.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM Playlist
                    )
                    UPDATE Playlist
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE Playlist.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM ProgramSchedule
                    )
                    UPDATE ProgramSchedule
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE ProgramSchedule.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM RerunCollection
                    )
                    UPDATE RerunCollection
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE RerunCollection.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
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
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM Template
                    )
                    UPDATE Template
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE Template.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);

            migrationBuilder.Sql(
                """
                    WITH Numbered AS (
                        SELECT
                            Id,
                            -- Partition by case-insensitive name
                            ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNum
                        FROM TemplateGroup
                    )
                    UPDATE TemplateGroup
                    SET Name = Name || ' (' || (Numbered.RowNum - 1) || ')'
                    FROM Numbered
                    WHERE TemplateGroup.Id = Numbered.Id
                      AND Numbered.RowNum > 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
