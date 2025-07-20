using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_RemoteStream : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RemoteStreamMetadataId",
                table: "Tag",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemoteStreamMetadataId",
                table: "Subtitle",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemoteStreamMetadataId",
                table: "Studio",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemoteStreamMetadataId",
                table: "MetadataGuid",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemoteStreamId",
                table: "MediaVersion",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemoteStreamMetadataId",
                table: "Genre",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemoteStreamMetadataId",
                table: "Artwork",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemoteStreamMetadataId",
                table: "Actor",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RemoteStream",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteStream", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemoteStream_MediaItem_Id",
                        column: x => x.Id,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RemoteStreamMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RemoteStreamId = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataKind = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalTitle = table.Column<string>(type: "TEXT", nullable: true),
                    SortTitle = table.Column<string>(type: "TEXT", nullable: true),
                    Year = table.Column<int>(type: "INTEGER", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteStreamMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemoteStreamMetadata_RemoteStream_RemoteStreamId",
                        column: x => x.RemoteStreamId,
                        principalTable: "RemoteStream",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tag_RemoteStreamMetadataId",
                table: "Tag",
                column: "RemoteStreamMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_RemoteStreamMetadataId",
                table: "Subtitle",
                column: "RemoteStreamMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Studio_RemoteStreamMetadataId",
                table: "Studio",
                column: "RemoteStreamMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_RemoteStreamMetadataId",
                table: "MetadataGuid",
                column: "RemoteStreamMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaVersion_RemoteStreamId",
                table: "MediaVersion",
                column: "RemoteStreamId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_RemoteStreamMetadataId",
                table: "Genre",
                column: "RemoteStreamMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_RemoteStreamMetadataId",
                table: "Artwork",
                column: "RemoteStreamMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_RemoteStreamMetadataId",
                table: "Actor",
                column: "RemoteStreamMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_RemoteStreamMetadata_RemoteStreamId",
                table: "RemoteStreamMetadata",
                column: "RemoteStreamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "Actor",
                column: "RemoteStreamMetadataId",
                principalTable: "RemoteStreamMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Artwork_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "Artwork",
                column: "RemoteStreamMetadataId",
                principalTable: "RemoteStreamMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "Genre",
                column: "RemoteStreamMetadataId",
                principalTable: "RemoteStreamMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaVersion_RemoteStream_RemoteStreamId",
                table: "MediaVersion",
                column: "RemoteStreamId",
                principalTable: "RemoteStream",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataGuid_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "MetadataGuid",
                column: "RemoteStreamMetadataId",
                principalTable: "RemoteStreamMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "Studio",
                column: "RemoteStreamMetadataId",
                principalTable: "RemoteStreamMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subtitle_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "Subtitle",
                column: "RemoteStreamMetadataId",
                principalTable: "RemoteStreamMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "Tag",
                column: "RemoteStreamMetadataId",
                principalTable: "RemoteStreamMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actor_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Artwork_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "Artwork");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaVersion_RemoteStream_RemoteStreamId",
                table: "MediaVersion");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataGuid_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "Studio");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtitle_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "Subtitle");

            migrationBuilder.DropForeignKey(
                name: "FK_Tag_RemoteStreamMetadata_RemoteStreamMetadataId",
                table: "Tag");

            migrationBuilder.DropTable(
                name: "RemoteStreamMetadata");

            migrationBuilder.DropTable(
                name: "RemoteStream");

            migrationBuilder.DropIndex(
                name: "IX_Tag_RemoteStreamMetadataId",
                table: "Tag");

            migrationBuilder.DropIndex(
                name: "IX_Subtitle_RemoteStreamMetadataId",
                table: "Subtitle");

            migrationBuilder.DropIndex(
                name: "IX_Studio_RemoteStreamMetadataId",
                table: "Studio");

            migrationBuilder.DropIndex(
                name: "IX_MetadataGuid_RemoteStreamMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropIndex(
                name: "IX_MediaVersion_RemoteStreamId",
                table: "MediaVersion");

            migrationBuilder.DropIndex(
                name: "IX_Genre_RemoteStreamMetadataId",
                table: "Genre");

            migrationBuilder.DropIndex(
                name: "IX_Artwork_RemoteStreamMetadataId",
                table: "Artwork");

            migrationBuilder.DropIndex(
                name: "IX_Actor_RemoteStreamMetadataId",
                table: "Actor");

            migrationBuilder.DropColumn(
                name: "RemoteStreamMetadataId",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "RemoteStreamMetadataId",
                table: "Subtitle");

            migrationBuilder.DropColumn(
                name: "RemoteStreamMetadataId",
                table: "Studio");

            migrationBuilder.DropColumn(
                name: "RemoteStreamMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropColumn(
                name: "RemoteStreamId",
                table: "MediaVersion");

            migrationBuilder.DropColumn(
                name: "RemoteStreamMetadataId",
                table: "Genre");

            migrationBuilder.DropColumn(
                name: "RemoteStreamMetadataId",
                table: "Artwork");

            migrationBuilder.DropColumn(
                name: "RemoteStreamMetadataId",
                table: "Actor");
        }
    }
}
