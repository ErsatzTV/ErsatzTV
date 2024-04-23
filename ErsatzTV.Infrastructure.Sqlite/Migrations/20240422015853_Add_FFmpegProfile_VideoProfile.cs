using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_FFmpegProfile_VideoProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoProfile",
                table: "FFmpegProfile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("UPDATE FFmpegProfile SET VideoProfile = 'high'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoProfile",
                table: "FFmpegProfile");
        }
    }
}
