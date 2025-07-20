using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_LocalLibraryRemoteStream : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // create local remote streams library
            migrationBuilder.Sql(
                @"INSERT INTO Library (Name, MediaKind, MediaSourceId)
                SELECT 'Remote Streams', 7, Id FROM
                (SELECT LMS.Id FROM LocalMediaSource LMS LIMIT 1)");
            migrationBuilder.Sql("INSERT INTO LocalLibrary (Id) Values (last_insert_rowid())");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
