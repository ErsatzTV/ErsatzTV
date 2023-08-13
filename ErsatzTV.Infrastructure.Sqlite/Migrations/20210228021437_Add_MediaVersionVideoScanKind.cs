using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_MediaVersionVideoScanKind : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.RenameColumn(
                "IsInterlaced",
                "MediaVersion",
                "VideoScanKind");

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.RenameColumn(
                "VideoScanKind",
                "MediaVersion",
                "IsInterlaced");
    }
}
