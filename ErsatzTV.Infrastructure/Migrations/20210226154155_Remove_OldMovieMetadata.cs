using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Remove_OldMovieMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "MovieMetadata");

            migrationBuilder.DropColumn(
                "MetadataId",
                "Movie");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                "MetadataId",
                "Movie",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                "MovieMetadata",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContentRating = table.Column<string>("TEXT", nullable: true),
                    LastWriteTime = table.Column<DateTime>("TEXT", nullable: true),
                    MovieId = table.Column<int>("INTEGER", nullable: false),
                    Outline = table.Column<string>("TEXT", nullable: true),
                    Plot = table.Column<string>("TEXT", nullable: true),
                    Premiered = table.Column<DateTime>("TEXT", nullable: true),
                    SortTitle = table.Column<string>("TEXT", nullable: true),
                    Source = table.Column<int>("INTEGER", nullable: false),
                    Tagline = table.Column<string>("TEXT", nullable: true),
                    Title = table.Column<string>("TEXT", nullable: true),
                    Year = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieMetadata", x => x.Id);
                    table.ForeignKey(
                        "FK_MovieMetadata_Movie_MovieId",
                        x => x.MovieId,
                        "Movie",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_MovieMetadata_MovieId",
                "MovieMetadata",
                "MovieId",
                unique: true);
        }
    }
}
