using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class InitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO MediaSource VALUES (NULL)");
            migrationBuilder.Sql("INSERT INTO LocalMediaSource (Id) SELECT last_insert_id()");
            
            // create local movies library
            migrationBuilder.Sql(
                @"INSERT INTO Library (Name, MediaKind, MediaSourceId)
            SELECT 'Movies', 1, Id FROM LocalMediaSource");
            migrationBuilder.Sql("INSERT INTO LocalLibrary (Id) Values (last_insert_id())");

            // create local shows library
            migrationBuilder.Sql(
                @"INSERT INTO Library (Name, MediaKind, MediaSourceId)
            SELECT 'Shows', 2, Id FROM LocalMediaSource");
            migrationBuilder.Sql("INSERT INTO LocalLibrary (Id) Values (last_insert_id())");
            
            // create local music videos library
            migrationBuilder.Sql(
                @"INSERT INTO Library (Name, MediaKind, MediaSourceId)
                SELECT 'Music Videos', 3, Id FROM
                (SELECT LMS.Id FROM LocalMediaSource LMS
                INNER JOIN Library L on L.MediaSourceId = LMS.Id
                INNER JOIN LocalLibrary LL on L.Id = LL.Id
                WHERE L.Name = 'Movies') AS A");
            migrationBuilder.Sql("INSERT INTO LocalLibrary (Id) Values (last_insert_id())");

            migrationBuilder.Sql(
                "INSERT INTO ConfigElement (`Key`, Value) VALUES ('scanner.library_refresh_interval', '6')");
            
            // create local other videos library
            migrationBuilder.Sql(
                @"INSERT INTO Library (Name, MediaKind, MediaSourceId)
                SELECT 'Other Videos', 4, Id FROM
                (SELECT LMS.Id FROM LocalMediaSource LMS LIMIT 1) AS A");
            migrationBuilder.Sql("INSERT INTO LocalLibrary (Id) Values (last_insert_id())");
            
            // create local songs library
            migrationBuilder.Sql(
                @"INSERT INTO Library (Name, MediaKind, MediaSourceId)
                SELECT 'Songs', 5, Id FROM
                (SELECT LMS.Id FROM LocalMediaSource LMS LIMIT 1) AS A");
            migrationBuilder.Sql("INSERT INTO LocalLibrary (Id) Values (last_insert_id())");
            
            migrationBuilder.Sql("INSERT INTO Resolution (Id, Height, Width, Name) VALUES (0, 480, 640, '640x480')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
