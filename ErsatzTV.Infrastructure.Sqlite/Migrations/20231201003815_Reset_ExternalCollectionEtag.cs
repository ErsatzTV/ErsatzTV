using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Reset_ExternalCollectionEtag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE PlexCollection SET Etag = NULL");
            migrationBuilder.Sql("UPDATE PlexMediaSource SET LastCollectionsScan = NULL");

            migrationBuilder.Sql("UPDATE EmbyCollection SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyMediaSource SET LastCollectionsScan = NULL");

            migrationBuilder.Sql("UPDATE JellyfinCollection SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinMediaSource SET LastCollectionsScan = NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
