using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_ChannelWatermark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WatermarkId",
                table: "Channel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChannelWatermark",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Location = table.Column<int>(type: "INTEGER", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    WidthPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    HorizontalMarginPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    VerticalMarginPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    FrequencyMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelWatermark", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Channel_WatermarkId",
                table: "Channel",
                column: "WatermarkId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_ChannelWatermark_WatermarkId",
                table: "Channel",
                column: "WatermarkId",
                principalTable: "ChannelWatermark",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channel_ChannelWatermark_WatermarkId",
                table: "Channel");

            migrationBuilder.DropTable(
                name: "ChannelWatermark");

            migrationBuilder.DropIndex(
                name: "IX_Channel_WatermarkId",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "WatermarkId",
                table: "Channel");
        }
    }
}
