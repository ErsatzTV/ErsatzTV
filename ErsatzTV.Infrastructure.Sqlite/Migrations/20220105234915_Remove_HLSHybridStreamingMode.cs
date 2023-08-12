using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Remove_HLSHybridStreamingMode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // replace HLS Hybrid with HLS Segmenter
            migrationBuilder.Sql("UPDATE Channel SET StreamingMode = 4 WHERE StreamingMode = 3");

            // replace MPEG-TS (Legacy) with new MPEG-TS
            migrationBuilder.Sql("UPDATE Channel SET StreamingMode = 5 WHERE StreamingMode = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
