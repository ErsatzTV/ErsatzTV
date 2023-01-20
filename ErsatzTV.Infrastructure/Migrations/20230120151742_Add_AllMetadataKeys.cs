using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAllMetadataKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actor_ArtistMetadata_ArtistMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Actor_MusicVideoMetadata_MusicVideoMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Actor_SeasonMetadata_SeasonMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Actor_SongMetadata_SongMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_EpisodeMetadata_EpisodeMetadataId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_SeasonMetadata_SeasonMetadataId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataGuid_ArtistMetadata_ArtistMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataGuid_MusicVideoMetadata_MusicVideoMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataGuid_SongMetadata_SongMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_ArtistMetadata_ArtistMetadataId",
                table: "Studio");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_EpisodeMetadata_EpisodeMetadataId",
                table: "Studio");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_SeasonMetadata_SeasonMetadataId",
                table: "Studio");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtitle_ArtistMetadata_ArtistMetadataId",
                table: "Subtitle");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtitle_SeasonMetadata_SeasonMetadataId",
                table: "Subtitle");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtitle_ShowMetadata_ShowMetadataId",
                table: "Subtitle");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtitle_SongMetadata_SongMetadataId",
                table: "Subtitle");

            migrationBuilder.DropForeignKey(
                name: "FK_Tag_ArtistMetadata_ArtistMetadataId",
                table: "Tag");

            migrationBuilder.DropForeignKey(
                name: "FK_Tag_EpisodeMetadata_EpisodeMetadataId",
                table: "Tag");

            migrationBuilder.DropForeignKey(
                name: "FK_Tag_SeasonMetadata_SeasonMetadataId",
                table: "Tag");

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_ArtistMetadata_ArtistMetadataId",
                table: "Actor",
                column: "ArtistMetadataId",
                principalTable: "ArtistMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_MusicVideoMetadata_MusicVideoMetadataId",
                table: "Actor",
                column: "MusicVideoMetadataId",
                principalTable: "MusicVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_SeasonMetadata_SeasonMetadataId",
                table: "Actor",
                column: "SeasonMetadataId",
                principalTable: "SeasonMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_SongMetadata_SongMetadataId",
                table: "Actor",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_EpisodeMetadata_EpisodeMetadataId",
                table: "Genre",
                column: "EpisodeMetadataId",
                principalTable: "EpisodeMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_SeasonMetadata_SeasonMetadataId",
                table: "Genre",
                column: "SeasonMetadataId",
                principalTable: "SeasonMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataGuid_ArtistMetadata_ArtistMetadataId",
                table: "MetadataGuid",
                column: "ArtistMetadataId",
                principalTable: "ArtistMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataGuid_MusicVideoMetadata_MusicVideoMetadataId",
                table: "MetadataGuid",
                column: "MusicVideoMetadataId",
                principalTable: "MusicVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataGuid_SongMetadata_SongMetadataId",
                table: "MetadataGuid",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_ArtistMetadata_ArtistMetadataId",
                table: "Studio",
                column: "ArtistMetadataId",
                principalTable: "ArtistMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_EpisodeMetadata_EpisodeMetadataId",
                table: "Studio",
                column: "EpisodeMetadataId",
                principalTable: "EpisodeMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_SeasonMetadata_SeasonMetadataId",
                table: "Studio",
                column: "SeasonMetadataId",
                principalTable: "SeasonMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subtitle_ArtistMetadata_ArtistMetadataId",
                table: "Subtitle",
                column: "ArtistMetadataId",
                principalTable: "ArtistMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subtitle_SeasonMetadata_SeasonMetadataId",
                table: "Subtitle",
                column: "SeasonMetadataId",
                principalTable: "SeasonMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subtitle_ShowMetadata_ShowMetadataId",
                table: "Subtitle",
                column: "ShowMetadataId",
                principalTable: "ShowMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subtitle_SongMetadata_SongMetadataId",
                table: "Subtitle",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_ArtistMetadata_ArtistMetadataId",
                table: "Tag",
                column: "ArtistMetadataId",
                principalTable: "ArtistMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_EpisodeMetadata_EpisodeMetadataId",
                table: "Tag",
                column: "EpisodeMetadataId",
                principalTable: "EpisodeMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_SeasonMetadata_SeasonMetadataId",
                table: "Tag",
                column: "SeasonMetadataId",
                principalTable: "SeasonMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actor_ArtistMetadata_ArtistMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Actor_MusicVideoMetadata_MusicVideoMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Actor_SeasonMetadata_SeasonMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Actor_SongMetadata_SongMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_EpisodeMetadata_EpisodeMetadataId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_SeasonMetadata_SeasonMetadataId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataGuid_ArtistMetadata_ArtistMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataGuid_MusicVideoMetadata_MusicVideoMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataGuid_SongMetadata_SongMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_ArtistMetadata_ArtistMetadataId",
                table: "Studio");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_EpisodeMetadata_EpisodeMetadataId",
                table: "Studio");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_SeasonMetadata_SeasonMetadataId",
                table: "Studio");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtitle_ArtistMetadata_ArtistMetadataId",
                table: "Subtitle");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtitle_SeasonMetadata_SeasonMetadataId",
                table: "Subtitle");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtitle_ShowMetadata_ShowMetadataId",
                table: "Subtitle");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtitle_SongMetadata_SongMetadataId",
                table: "Subtitle");

            migrationBuilder.DropForeignKey(
                name: "FK_Tag_ArtistMetadata_ArtistMetadataId",
                table: "Tag");

            migrationBuilder.DropForeignKey(
                name: "FK_Tag_EpisodeMetadata_EpisodeMetadataId",
                table: "Tag");

            migrationBuilder.DropForeignKey(
                name: "FK_Tag_SeasonMetadata_SeasonMetadataId",
                table: "Tag");

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_ArtistMetadata_ArtistMetadataId",
                table: "Actor",
                column: "ArtistMetadataId",
                principalTable: "ArtistMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_MusicVideoMetadata_MusicVideoMetadataId",
                table: "Actor",
                column: "MusicVideoMetadataId",
                principalTable: "MusicVideoMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_SeasonMetadata_SeasonMetadataId",
                table: "Actor",
                column: "SeasonMetadataId",
                principalTable: "SeasonMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_SongMetadata_SongMetadataId",
                table: "Actor",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_EpisodeMetadata_EpisodeMetadataId",
                table: "Genre",
                column: "EpisodeMetadataId",
                principalTable: "EpisodeMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_SeasonMetadata_SeasonMetadataId",
                table: "Genre",
                column: "SeasonMetadataId",
                principalTable: "SeasonMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataGuid_ArtistMetadata_ArtistMetadataId",
                table: "MetadataGuid",
                column: "ArtistMetadataId",
                principalTable: "ArtistMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataGuid_MusicVideoMetadata_MusicVideoMetadataId",
                table: "MetadataGuid",
                column: "MusicVideoMetadataId",
                principalTable: "MusicVideoMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataGuid_SongMetadata_SongMetadataId",
                table: "MetadataGuid",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_ArtistMetadata_ArtistMetadataId",
                table: "Studio",
                column: "ArtistMetadataId",
                principalTable: "ArtistMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_EpisodeMetadata_EpisodeMetadataId",
                table: "Studio",
                column: "EpisodeMetadataId",
                principalTable: "EpisodeMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_SeasonMetadata_SeasonMetadataId",
                table: "Studio",
                column: "SeasonMetadataId",
                principalTable: "SeasonMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subtitle_ArtistMetadata_ArtistMetadataId",
                table: "Subtitle",
                column: "ArtistMetadataId",
                principalTable: "ArtistMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subtitle_SeasonMetadata_SeasonMetadataId",
                table: "Subtitle",
                column: "SeasonMetadataId",
                principalTable: "SeasonMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subtitle_ShowMetadata_ShowMetadataId",
                table: "Subtitle",
                column: "ShowMetadataId",
                principalTable: "ShowMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subtitle_SongMetadata_SongMetadataId",
                table: "Subtitle",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_ArtistMetadata_ArtistMetadataId",
                table: "Tag",
                column: "ArtistMetadataId",
                principalTable: "ArtistMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_EpisodeMetadata_EpisodeMetadataId",
                table: "Tag",
                column: "EpisodeMetadataId",
                principalTable: "EpisodeMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_SeasonMetadata_SeasonMetadataId",
                table: "Tag",
                column: "SeasonMetadataId",
                principalTable: "SeasonMetadata",
                principalColumn: "Id");
        }
    }
}
