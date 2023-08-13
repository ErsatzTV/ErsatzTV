using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_LocalLibrary_MusicVideos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // create local music videos library
            migrationBuilder.Sql(
                @"INSERT INTO Library (Name, MediaKind, MediaSourceId)
                SELECT 'Music Videos', 3, Id FROM
                (SELECT LMS.Id FROM LocalMediaSource LMS
                INNER JOIN Library L on L.MediaSourceId = LMS.Id
                INNER JOIN LocalLibrary LL on L.Id = LL.Id
                WHERE L.Name = 'Movies')");
            migrationBuilder.Sql("INSERT INTO LocalLibrary (Id) Values (last_insert_rowid())");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
