using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Update_MediaVersion_SampleAspectRatio11 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
            @"UPDATE MediaVersion SET SampleAspectRatio = '1:1' where SampleAspectRatio is null");

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
