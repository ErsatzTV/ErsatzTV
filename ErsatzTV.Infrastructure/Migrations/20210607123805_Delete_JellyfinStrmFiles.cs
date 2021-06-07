using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Delete_JellyfinStrmFiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DELETE FROM MediaItem WHERE Id IN
                    (SELECT MI.Id FROM MediaItem MI
                    INNER JOIN MediaVersion MV on MV.MovieId = MI.Id
                    INNER JOIN MediaFile MF on MV.Id = MF.MediaVersionId
                    WHERE MF.Path LIKE '%.strm')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
