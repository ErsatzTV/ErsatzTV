using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_PlexMediaFile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_PlexMovie_PlexMediaItemPart_PartId",
                "PlexMovie");

            migrationBuilder.DropTable(
                "PlexMediaItemPart");

            migrationBuilder.DropIndex(
                "IX_PlexMovie_PartId",
                "PlexMovie");

            migrationBuilder.DropColumn(
                "PartId",
                "PlexMovie");

            migrationBuilder.CreateTable(
                "PlexMediaFile",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlexId = table.Column<int>("INTEGER", nullable: false),
                    Key = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexMediaFile", x => x.Id);
                    table.ForeignKey(
                        "FK_PlexMediaFile_MediaFile_Id",
                        x => x.Id,
                        "MediaFile",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "PlexMediaFile");

            migrationBuilder.AddColumn<int>(
                "PartId",
                "PlexMovie",
                "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                "PlexMediaItemPart",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Duration = table.Column<int>("INTEGER", nullable: false),
                    File = table.Column<string>("TEXT", nullable: true),
                    Key = table.Column<string>("TEXT", nullable: true),
                    PlexId = table.Column<int>("INTEGER", nullable: false),
                    Size = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_PlexMediaItemPart", x => x.Id); });

            migrationBuilder.CreateIndex(
                "IX_PlexMovie_PartId",
                "PlexMovie",
                "PartId");

            migrationBuilder.AddForeignKey(
                "FK_PlexMovie_PlexMediaItemPart_PartId",
                "PlexMovie",
                "PartId",
                "PlexMediaItemPart",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
