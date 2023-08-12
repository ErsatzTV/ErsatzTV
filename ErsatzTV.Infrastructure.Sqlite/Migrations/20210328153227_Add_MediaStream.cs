using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_MediaStream : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "MediaStream",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Index = table.Column<int>("INTEGER", nullable: false),
                    Codec = table.Column<string>("TEXT", nullable: true),
                    Profile = table.Column<string>("TEXT", nullable: true),
                    MediaStreamKind = table.Column<int>("INTEGER", nullable: false),
                    Language = table.Column<string>("TEXT", nullable: true),
                    Channels = table.Column<int>("INTEGER", nullable: false),
                    Title = table.Column<string>("TEXT", nullable: true),
                    Default = table.Column<bool>("INTEGER", nullable: false),
                    Forced = table.Column<bool>("INTEGER", nullable: false),
                    MediaVersionId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaStream", x => x.Id);
                    table.ForeignKey(
                        "FK_MediaStream_MediaVersion_MediaVersionId",
                        x => x.MediaVersionId,
                        "MediaVersion",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_MediaStream_MediaVersionId",
                "MediaStream",
                "MediaVersionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(
                "MediaStream");
    }
}
