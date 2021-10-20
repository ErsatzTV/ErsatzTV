using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Update_PlayoutItemFillerKind : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE PlayoutItem SET FillerKind = 4 WHERE IsFiller = 1");
            migrationBuilder.Sql(@"UPDATE PlayoutItem SET FillerKind = 5 WHERE IsFallback = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
