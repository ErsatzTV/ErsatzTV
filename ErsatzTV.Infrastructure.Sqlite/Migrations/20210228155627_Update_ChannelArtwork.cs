using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Update_ChannelArtwork : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);

            migrationBuilder.Sql(
                $@"INSERT INTO Artwork (ArtworkKind, ChannelId, DateAdded, DateUpdated, Path)
                SELECT 2, c.Id, '{now}', '{now}', c.Logo
                FROM Channel c
                WHERE c.Logo is not null");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
