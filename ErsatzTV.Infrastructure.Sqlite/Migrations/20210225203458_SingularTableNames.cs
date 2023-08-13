using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class SingularTableNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Channels_FFmpegProfiles_FFmpegProfileId",
                "Channels");

            migrationBuilder.DropForeignKey(
                "FK_FFmpegProfiles_Resolutions_ResolutionId",
                "FFmpegProfiles");

            migrationBuilder.DropForeignKey(
                "FK_Library_MediaSources_MediaSourceId",
                "Library");

            migrationBuilder.DropForeignKey(
                "FK_LocalMediaSource_MediaSources_Id",
                "LocalMediaSource");

            migrationBuilder.DropForeignKey(
                "FK_MediaItems_LibraryPath_LibraryPathId",
                "MediaItems");

            migrationBuilder.DropForeignKey(
                "FK_MovieMetadata_Movies_MovieId",
                "MovieMetadata");

            migrationBuilder.DropForeignKey(
                "FK_MovieMetadata_Movies_MovieId1",
                "MovieMetadata");

            migrationBuilder.DropForeignKey(
                "FK_Movies_MediaItems_Id",
                "Movies");

            migrationBuilder.DropForeignKey(
                "FK_PlayoutItems_MediaItems_MediaItemId",
                "PlayoutItems");

            migrationBuilder.DropForeignKey(
                "FK_PlayoutItems_Playouts_PlayoutId",
                "PlayoutItems");

            migrationBuilder.DropForeignKey(
                "FK_PlayoutProgramScheduleItemAnchors_Playouts_PlayoutId",
                "PlayoutProgramScheduleItemAnchors");

            migrationBuilder.DropForeignKey(
                "FK_PlayoutProgramScheduleItemAnchors_ProgramSchedules_ProgramScheduleId",
                "PlayoutProgramScheduleItemAnchors");

            migrationBuilder.DropForeignKey(
                "FK_Playouts_Channels_ChannelId",
                "Playouts");

            migrationBuilder.DropForeignKey(
                "FK_Playouts_ProgramScheduleItems_Anchor_NextScheduleItemId",
                "Playouts");

            migrationBuilder.DropForeignKey(
                "FK_Playouts_ProgramSchedules_ProgramScheduleId",
                "Playouts");

            migrationBuilder.DropForeignKey(
                "FK_PlexMediaSource_MediaSources_Id",
                "PlexMediaSource");

            migrationBuilder.DropForeignKey(
                "FK_PlexMovies_Movies_Id",
                "PlexMovies");

            migrationBuilder.DropForeignKey(
                "FK_PlexMovies_PlexMediaItemPart_PartId",
                "PlexMovies");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleDurationItems_ProgramScheduleItems_Id",
                "ProgramScheduleDurationItems");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleFloodItems_ProgramScheduleItems_Id",
                "ProgramScheduleFloodItems");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItems_MediaCollections_MediaCollectionId",
                "ProgramScheduleItems");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItems_ProgramSchedules_ProgramScheduleId",
                "ProgramScheduleItems");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItems_TelevisionSeasons_TelevisionSeasonId",
                "ProgramScheduleItems");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItems_TelevisionShows_TelevisionShowId",
                "ProgramScheduleItems");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleMultipleItems_ProgramScheduleItems_Id",
                "ProgramScheduleMultipleItems");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleOneItems_ProgramScheduleItems_Id",
                "ProgramScheduleOneItems");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionEpisodes_SimpleMediaCollections_SimpleMediaCollectionsId",
                "SimpleMediaCollectionEpisodes");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionEpisodes_TelevisionEpisodes_TelevisionEpisodesId",
                "SimpleMediaCollectionEpisodes");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionMovies_Movies_MoviesId",
                "SimpleMediaCollectionMovies");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionMovies_SimpleMediaCollections_SimpleMediaCollectionsId",
                "SimpleMediaCollectionMovies");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollections_MediaCollections_Id",
                "SimpleMediaCollections");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionSeasons_SimpleMediaCollections_SimpleMediaCollectionsId",
                "SimpleMediaCollectionSeasons");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionSeasons_TelevisionSeasons_TelevisionSeasonsId",
                "SimpleMediaCollectionSeasons");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionShows_SimpleMediaCollections_SimpleMediaCollectionsId",
                "SimpleMediaCollectionShows");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionShows_TelevisionShows_TelevisionShowsId",
                "SimpleMediaCollectionShows");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionEpisodeMetadata_TelevisionEpisodes_TelevisionEpisodeId",
                "TelevisionEpisodeMetadata");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionEpisodes_MediaItems_Id",
                "TelevisionEpisodes");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionEpisodes_TelevisionSeasons_SeasonId",
                "TelevisionEpisodes");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionSeasons_TelevisionShows_TelevisionShowId",
                "TelevisionSeasons");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionShowMetadata_TelevisionShows_TelevisionShowId",
                "TelevisionShowMetadata");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionShowSource_TelevisionShows_TelevisionShowId",
                "TelevisionShowSource");

            migrationBuilder.DropIndex(
                "IX_MovieMetadata_MovieId1",
                "MovieMetadata");

            migrationBuilder.DropPrimaryKey(
                "PK_TelevisionShows",
                "TelevisionShows");

            migrationBuilder.DropPrimaryKey(
                "PK_TelevisionSeasons",
                "TelevisionSeasons");

            migrationBuilder.DropPrimaryKey(
                "PK_TelevisionEpisodes",
                "TelevisionEpisodes");

            migrationBuilder.DropPrimaryKey(
                "PK_SimpleMediaCollectionShows",
                "SimpleMediaCollectionShows");

            migrationBuilder.DropPrimaryKey(
                "PK_SimpleMediaCollectionSeasons",
                "SimpleMediaCollectionSeasons");

            migrationBuilder.DropPrimaryKey(
                "PK_SimpleMediaCollections",
                "SimpleMediaCollections");

            migrationBuilder.DropPrimaryKey(
                "PK_SimpleMediaCollectionMovies",
                "SimpleMediaCollectionMovies");

            migrationBuilder.DropPrimaryKey(
                "PK_SimpleMediaCollectionEpisodes",
                "SimpleMediaCollectionEpisodes");

            migrationBuilder.DropPrimaryKey(
                "PK_Resolutions",
                "Resolutions");

            migrationBuilder.DropPrimaryKey(
                "PK_ProgramSchedules",
                "ProgramSchedules");

            migrationBuilder.DropPrimaryKey(
                "PK_ProgramScheduleOneItems",
                "ProgramScheduleOneItems");

            migrationBuilder.DropPrimaryKey(
                "PK_ProgramScheduleMultipleItems",
                "ProgramScheduleMultipleItems");

            migrationBuilder.DropPrimaryKey(
                "PK_ProgramScheduleItems",
                "ProgramScheduleItems");

            migrationBuilder.DropPrimaryKey(
                "PK_ProgramScheduleFloodItems",
                "ProgramScheduleFloodItems");

            migrationBuilder.DropPrimaryKey(
                "PK_ProgramScheduleDurationItems",
                "ProgramScheduleDurationItems");

            migrationBuilder.DropPrimaryKey(
                "PK_PlexMovies",
                "PlexMovies");

            migrationBuilder.DropPrimaryKey(
                "PK_Playouts",
                "Playouts");

            migrationBuilder.DropPrimaryKey(
                "PK_PlayoutProgramScheduleItemAnchors",
                "PlayoutProgramScheduleItemAnchors");

            migrationBuilder.DropPrimaryKey(
                "PK_PlayoutItems",
                "PlayoutItems");

            migrationBuilder.DropPrimaryKey(
                "PK_Movies",
                "Movies");

            migrationBuilder.DropPrimaryKey(
                "PK_MediaSources",
                "MediaSources");

            migrationBuilder.DropPrimaryKey(
                "PK_MediaItems",
                "MediaItems");

            migrationBuilder.DropPrimaryKey(
                "PK_MediaCollections",
                "MediaCollections");

            migrationBuilder.DropPrimaryKey(
                "PK_FFmpegProfiles",
                "FFmpegProfiles");

            migrationBuilder.DropPrimaryKey(
                "PK_ConfigElements",
                "ConfigElements");

            migrationBuilder.DropPrimaryKey(
                "PK_Channels",
                "Channels");

            migrationBuilder.DropColumn(
                "MovieId1",
                "MovieMetadata");

            migrationBuilder.RenameTable(
                "TelevisionShows",
                newName: "TelevisionShow");

            migrationBuilder.RenameTable(
                "TelevisionSeasons",
                newName: "TelevisionSeason");

            migrationBuilder.RenameTable(
                "TelevisionEpisodes",
                newName: "TelevisionEpisode");

            migrationBuilder.RenameTable(
                "SimpleMediaCollectionShows",
                newName: "SimpleMediaCollectionShow");

            migrationBuilder.RenameTable(
                "SimpleMediaCollectionSeasons",
                newName: "SimpleMediaCollectionSeason");

            migrationBuilder.RenameTable(
                "SimpleMediaCollections",
                newName: "SimpleMediaCollection");

            migrationBuilder.RenameTable(
                "SimpleMediaCollectionMovies",
                newName: "SimpleMediaCollectionMovie");

            migrationBuilder.RenameTable(
                "SimpleMediaCollectionEpisodes",
                newName: "SimpleMediaCollectionEpisode");

            migrationBuilder.RenameTable(
                "Resolutions",
                newName: "Resolution");

            migrationBuilder.RenameTable(
                "ProgramSchedules",
                newName: "ProgramSchedule");

            migrationBuilder.RenameTable(
                "ProgramScheduleOneItems",
                newName: "ProgramScheduleOneItem");

            migrationBuilder.RenameTable(
                "ProgramScheduleMultipleItems",
                newName: "ProgramScheduleMultipleItem");

            migrationBuilder.RenameTable(
                "ProgramScheduleItems",
                newName: "ProgramScheduleItem");

            migrationBuilder.RenameTable(
                "ProgramScheduleFloodItems",
                newName: "ProgramScheduleFloodItem");

            migrationBuilder.RenameTable(
                "ProgramScheduleDurationItems",
                newName: "ProgramScheduleDurationItem");

            migrationBuilder.RenameTable(
                "PlexMovies",
                newName: "PlexMovie");

            migrationBuilder.RenameTable(
                "Playouts",
                newName: "Playout");

            migrationBuilder.RenameTable(
                "PlayoutProgramScheduleItemAnchors",
                newName: "PlayoutProgramScheduleAnchor");

            migrationBuilder.RenameTable(
                "PlayoutItems",
                newName: "PlayoutItem");

            migrationBuilder.RenameTable(
                "Movies",
                newName: "Movie");

            migrationBuilder.RenameTable(
                "MediaSources",
                newName: "MediaSource");

            migrationBuilder.RenameTable(
                "MediaItems",
                newName: "MediaItem");

            migrationBuilder.RenameTable(
                "MediaCollections",
                newName: "MediaCollection");

            migrationBuilder.RenameTable(
                "FFmpegProfiles",
                newName: "FFmpegProfile");

            migrationBuilder.RenameTable(
                "ConfigElements",
                newName: "ConfigElement");

            migrationBuilder.RenameTable(
                "Channels",
                newName: "Channel");

            migrationBuilder.RenameIndex(
                "IX_TelevisionSeasons_TelevisionShowId",
                table: "TelevisionSeason",
                newName: "IX_TelevisionSeason_TelevisionShowId");

            migrationBuilder.RenameIndex(
                "IX_TelevisionEpisodes_SeasonId",
                table: "TelevisionEpisode",
                newName: "IX_TelevisionEpisode_SeasonId");

            migrationBuilder.RenameIndex(
                "IX_SimpleMediaCollectionShows_TelevisionShowsId",
                table: "SimpleMediaCollectionShow",
                newName: "IX_SimpleMediaCollectionShow_TelevisionShowsId");

            migrationBuilder.RenameIndex(
                "IX_SimpleMediaCollectionSeasons_TelevisionSeasonsId",
                table: "SimpleMediaCollectionSeason",
                newName: "IX_SimpleMediaCollectionSeason_TelevisionSeasonsId");

            migrationBuilder.RenameIndex(
                "IX_SimpleMediaCollectionMovies_SimpleMediaCollectionsId",
                table: "SimpleMediaCollectionMovie",
                newName: "IX_SimpleMediaCollectionMovie_SimpleMediaCollectionsId");

            migrationBuilder.RenameIndex(
                "IX_SimpleMediaCollectionEpisodes_TelevisionEpisodesId",
                table: "SimpleMediaCollectionEpisode",
                newName: "IX_SimpleMediaCollectionEpisode_TelevisionEpisodesId");

            migrationBuilder.RenameIndex(
                "IX_ProgramSchedules_Name",
                table: "ProgramSchedule",
                newName: "IX_ProgramSchedule_Name");

            migrationBuilder.RenameIndex(
                "IX_ProgramScheduleItems_TelevisionShowId",
                table: "ProgramScheduleItem",
                newName: "IX_ProgramScheduleItem_TelevisionShowId");

            migrationBuilder.RenameIndex(
                "IX_ProgramScheduleItems_TelevisionSeasonId",
                table: "ProgramScheduleItem",
                newName: "IX_ProgramScheduleItem_TelevisionSeasonId");

            migrationBuilder.RenameIndex(
                "IX_ProgramScheduleItems_ProgramScheduleId",
                table: "ProgramScheduleItem",
                newName: "IX_ProgramScheduleItem_ProgramScheduleId");

            migrationBuilder.RenameIndex(
                "IX_ProgramScheduleItems_MediaCollectionId",
                table: "ProgramScheduleItem",
                newName: "IX_ProgramScheduleItem_MediaCollectionId");

            migrationBuilder.RenameIndex(
                "IX_PlexMovies_PartId",
                table: "PlexMovie",
                newName: "IX_PlexMovie_PartId");

            migrationBuilder.RenameIndex(
                "IX_Playouts_ProgramScheduleId",
                table: "Playout",
                newName: "IX_Playout_ProgramScheduleId");

            migrationBuilder.RenameIndex(
                "IX_Playouts_ChannelId",
                table: "Playout",
                newName: "IX_Playout_ChannelId");

            migrationBuilder.RenameIndex(
                "IX_Playouts_Anchor_NextScheduleItemId",
                table: "Playout",
                newName: "IX_Playout_Anchor_NextScheduleItemId");

            migrationBuilder.RenameIndex(
                "IX_PlayoutProgramScheduleItemAnchors_ProgramScheduleId",
                table: "PlayoutProgramScheduleAnchor",
                newName: "IX_PlayoutProgramScheduleAnchor_ProgramScheduleId");

            migrationBuilder.RenameIndex(
                "IX_PlayoutProgramScheduleItemAnchors_PlayoutId",
                table: "PlayoutProgramScheduleAnchor",
                newName: "IX_PlayoutProgramScheduleAnchor_PlayoutId");

            migrationBuilder.RenameIndex(
                "IX_PlayoutItems_PlayoutId",
                table: "PlayoutItem",
                newName: "IX_PlayoutItem_PlayoutId");

            migrationBuilder.RenameIndex(
                "IX_PlayoutItems_MediaItemId",
                table: "PlayoutItem",
                newName: "IX_PlayoutItem_MediaItemId");

            migrationBuilder.RenameIndex(
                "IX_MediaItems_LibraryPathId",
                table: "MediaItem",
                newName: "IX_MediaItem_LibraryPathId");

            migrationBuilder.RenameIndex(
                "IX_MediaCollections_Name",
                table: "MediaCollection",
                newName: "IX_MediaCollection_Name");

            migrationBuilder.RenameIndex(
                "IX_FFmpegProfiles_ResolutionId",
                table: "FFmpegProfile",
                newName: "IX_FFmpegProfile_ResolutionId");

            migrationBuilder.RenameIndex(
                "IX_ConfigElements_Key",
                table: "ConfigElement",
                newName: "IX_ConfigElement_Key");

            migrationBuilder.RenameIndex(
                "IX_Channels_Number",
                table: "Channel",
                newName: "IX_Channel_Number");

            migrationBuilder.RenameIndex(
                "IX_Channels_FFmpegProfileId",
                table: "Channel",
                newName: "IX_Channel_FFmpegProfileId");

            migrationBuilder.AddPrimaryKey(
                "PK_TelevisionShow",
                "TelevisionShow",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_TelevisionSeason",
                "TelevisionSeason",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_TelevisionEpisode",
                "TelevisionEpisode",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_SimpleMediaCollectionShow",
                "SimpleMediaCollectionShow",
                new[] { "SimpleMediaCollectionsId", "TelevisionShowsId" });

            migrationBuilder.AddPrimaryKey(
                "PK_SimpleMediaCollectionSeason",
                "SimpleMediaCollectionSeason",
                new[] { "SimpleMediaCollectionsId", "TelevisionSeasonsId" });

            migrationBuilder.AddPrimaryKey(
                "PK_SimpleMediaCollection",
                "SimpleMediaCollection",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_SimpleMediaCollectionMovie",
                "SimpleMediaCollectionMovie",
                new[] { "MoviesId", "SimpleMediaCollectionsId" });

            migrationBuilder.AddPrimaryKey(
                "PK_SimpleMediaCollectionEpisode",
                "SimpleMediaCollectionEpisode",
                new[] { "SimpleMediaCollectionsId", "TelevisionEpisodesId" });

            migrationBuilder.AddPrimaryKey(
                "PK_Resolution",
                "Resolution",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ProgramSchedule",
                "ProgramSchedule",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ProgramScheduleOneItem",
                "ProgramScheduleOneItem",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ProgramScheduleMultipleItem",
                "ProgramScheduleMultipleItem",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ProgramScheduleItem",
                "ProgramScheduleItem",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ProgramScheduleFloodItem",
                "ProgramScheduleFloodItem",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ProgramScheduleDurationItem",
                "ProgramScheduleDurationItem",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_PlexMovie",
                "PlexMovie",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_Playout",
                "Playout",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_PlayoutProgramScheduleAnchor",
                "PlayoutProgramScheduleAnchor",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_PlayoutItem",
                "PlayoutItem",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_Movie",
                "Movie",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_MediaSource",
                "MediaSource",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_MediaItem",
                "MediaItem",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_MediaCollection",
                "MediaCollection",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_FFmpegProfile",
                "FFmpegProfile",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ConfigElement",
                "ConfigElement",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_Channel",
                "Channel",
                "Id");

            migrationBuilder.AddForeignKey(
                "FK_Channel_FFmpegProfile_FFmpegProfileId",
                "Channel",
                "FFmpegProfileId",
                "FFmpegProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_FFmpegProfile_Resolution_ResolutionId",
                "FFmpegProfile",
                "ResolutionId",
                "Resolution",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Library_MediaSource_MediaSourceId",
                "Library",
                "MediaSourceId",
                "MediaSource",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_LocalMediaSource_MediaSource_Id",
                "LocalMediaSource",
                "Id",
                "MediaSource",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_MediaItem_LibraryPath_LibraryPathId",
                "MediaItem",
                "LibraryPathId",
                "LibraryPath",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Movie_MediaItem_Id",
                "Movie",
                "Id",
                "MediaItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_MovieMetadata_Movie_MovieId",
                "MovieMetadata",
                "MovieId",
                "Movie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Playout_Channel_ChannelId",
                "Playout",
                "ChannelId",
                "Channel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Playout_ProgramSchedule_ProgramScheduleId",
                "Playout",
                "ProgramScheduleId",
                "ProgramSchedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Playout_ProgramScheduleItem_Anchor_NextScheduleItemId",
                "Playout",
                "Anchor_NextScheduleItemId",
                "ProgramScheduleItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutItem_MediaItem_MediaItemId",
                "PlayoutItem",
                "MediaItemId",
                "MediaItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutItem_Playout_PlayoutId",
                "PlayoutItem",
                "PlayoutId",
                "Playout",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutProgramScheduleAnchor_Playout_PlayoutId",
                "PlayoutProgramScheduleAnchor",
                "PlayoutId",
                "Playout",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutProgramScheduleAnchor_ProgramSchedule_ProgramScheduleId",
                "PlayoutProgramScheduleAnchor",
                "ProgramScheduleId",
                "ProgramSchedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlexMediaSource_MediaSource_Id",
                "PlexMediaSource",
                "Id",
                "MediaSource",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlexMovie_Movie_Id",
                "PlexMovie",
                "Id",
                "Movie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlexMovie_PlexMediaItemPart_PartId",
                "PlexMovie",
                "PartId",
                "PlexMediaItemPart",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleDurationItem_ProgramScheduleItem_Id",
                "ProgramScheduleDurationItem",
                "Id",
                "ProgramScheduleItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleFloodItem_ProgramScheduleItem_Id",
                "ProgramScheduleFloodItem",
                "Id",
                "ProgramScheduleItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItem_MediaCollection_MediaCollectionId",
                "ProgramScheduleItem",
                "MediaCollectionId",
                "MediaCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItem_ProgramSchedule_ProgramScheduleId",
                "ProgramScheduleItem",
                "ProgramScheduleId",
                "ProgramSchedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItem_TelevisionSeason_TelevisionSeasonId",
                "ProgramScheduleItem",
                "TelevisionSeasonId",
                "TelevisionSeason",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItem_TelevisionShow_TelevisionShowId",
                "ProgramScheduleItem",
                "TelevisionShowId",
                "TelevisionShow",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleMultipleItem_ProgramScheduleItem_Id",
                "ProgramScheduleMultipleItem",
                "Id",
                "ProgramScheduleItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleOneItem_ProgramScheduleItem_Id",
                "ProgramScheduleOneItem",
                "Id",
                "ProgramScheduleItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollection_MediaCollection_Id",
                "SimpleMediaCollection",
                "Id",
                "MediaCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionEpisode_SimpleMediaCollection_SimpleMediaCollectionsId",
                "SimpleMediaCollectionEpisode",
                "SimpleMediaCollectionsId",
                "SimpleMediaCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionEpisode_TelevisionEpisode_TelevisionEpisodesId",
                "SimpleMediaCollectionEpisode",
                "TelevisionEpisodesId",
                "TelevisionEpisode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionMovie_Movie_MoviesId",
                "SimpleMediaCollectionMovie",
                "MoviesId",
                "Movie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionMovie_SimpleMediaCollection_SimpleMediaCollectionsId",
                "SimpleMediaCollectionMovie",
                "SimpleMediaCollectionsId",
                "SimpleMediaCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionSeason_SimpleMediaCollection_SimpleMediaCollectionsId",
                "SimpleMediaCollectionSeason",
                "SimpleMediaCollectionsId",
                "SimpleMediaCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionSeason_TelevisionSeason_TelevisionSeasonsId",
                "SimpleMediaCollectionSeason",
                "TelevisionSeasonsId",
                "TelevisionSeason",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionShow_SimpleMediaCollection_SimpleMediaCollectionsId",
                "SimpleMediaCollectionShow",
                "SimpleMediaCollectionsId",
                "SimpleMediaCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionShow_TelevisionShow_TelevisionShowsId",
                "SimpleMediaCollectionShow",
                "TelevisionShowsId",
                "TelevisionShow",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionEpisode_MediaItem_Id",
                "TelevisionEpisode",
                "Id",
                "MediaItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionEpisode_TelevisionSeason_SeasonId",
                "TelevisionEpisode",
                "SeasonId",
                "TelevisionSeason",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionEpisodeMetadata_TelevisionEpisode_TelevisionEpisodeId",
                "TelevisionEpisodeMetadata",
                "TelevisionEpisodeId",
                "TelevisionEpisode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionSeason_TelevisionShow_TelevisionShowId",
                "TelevisionSeason",
                "TelevisionShowId",
                "TelevisionShow",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionShowMetadata_TelevisionShow_TelevisionShowId",
                "TelevisionShowMetadata",
                "TelevisionShowId",
                "TelevisionShow",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionShowSource_TelevisionShow_TelevisionShowId",
                "TelevisionShowSource",
                "TelevisionShowId",
                "TelevisionShow",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Channel_FFmpegProfile_FFmpegProfileId",
                "Channel");

            migrationBuilder.DropForeignKey(
                "FK_FFmpegProfile_Resolution_ResolutionId",
                "FFmpegProfile");

            migrationBuilder.DropForeignKey(
                "FK_Library_MediaSource_MediaSourceId",
                "Library");

            migrationBuilder.DropForeignKey(
                "FK_LocalMediaSource_MediaSource_Id",
                "LocalMediaSource");

            migrationBuilder.DropForeignKey(
                "FK_MediaItem_LibraryPath_LibraryPathId",
                "MediaItem");

            migrationBuilder.DropForeignKey(
                "FK_Movie_MediaItem_Id",
                "Movie");

            migrationBuilder.DropForeignKey(
                "FK_MovieMetadata_Movie_MovieId",
                "MovieMetadata");

            migrationBuilder.DropForeignKey(
                "FK_Playout_Channel_ChannelId",
                "Playout");

            migrationBuilder.DropForeignKey(
                "FK_Playout_ProgramSchedule_ProgramScheduleId",
                "Playout");

            migrationBuilder.DropForeignKey(
                "FK_Playout_ProgramScheduleItem_Anchor_NextScheduleItemId",
                "Playout");

            migrationBuilder.DropForeignKey(
                "FK_PlayoutItem_MediaItem_MediaItemId",
                "PlayoutItem");

            migrationBuilder.DropForeignKey(
                "FK_PlayoutItem_Playout_PlayoutId",
                "PlayoutItem");

            migrationBuilder.DropForeignKey(
                "FK_PlayoutProgramScheduleAnchor_Playout_PlayoutId",
                "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropForeignKey(
                "FK_PlayoutProgramScheduleAnchor_ProgramSchedule_ProgramScheduleId",
                "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropForeignKey(
                "FK_PlexMediaSource_MediaSource_Id",
                "PlexMediaSource");

            migrationBuilder.DropForeignKey(
                "FK_PlexMovie_Movie_Id",
                "PlexMovie");

            migrationBuilder.DropForeignKey(
                "FK_PlexMovie_PlexMediaItemPart_PartId",
                "PlexMovie");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleDurationItem_ProgramScheduleItem_Id",
                "ProgramScheduleDurationItem");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleFloodItem_ProgramScheduleItem_Id",
                "ProgramScheduleFloodItem");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItem_MediaCollection_MediaCollectionId",
                "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItem_ProgramSchedule_ProgramScheduleId",
                "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItem_TelevisionSeason_TelevisionSeasonId",
                "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItem_TelevisionShow_TelevisionShowId",
                "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleMultipleItem_ProgramScheduleItem_Id",
                "ProgramScheduleMultipleItem");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleOneItem_ProgramScheduleItem_Id",
                "ProgramScheduleOneItem");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollection_MediaCollection_Id",
                "SimpleMediaCollection");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionEpisode_SimpleMediaCollection_SimpleMediaCollectionsId",
                "SimpleMediaCollectionEpisode");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionEpisode_TelevisionEpisode_TelevisionEpisodesId",
                "SimpleMediaCollectionEpisode");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionMovie_Movie_MoviesId",
                "SimpleMediaCollectionMovie");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionMovie_SimpleMediaCollection_SimpleMediaCollectionsId",
                "SimpleMediaCollectionMovie");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionSeason_SimpleMediaCollection_SimpleMediaCollectionsId",
                "SimpleMediaCollectionSeason");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionSeason_TelevisionSeason_TelevisionSeasonsId",
                "SimpleMediaCollectionSeason");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionShow_SimpleMediaCollection_SimpleMediaCollectionsId",
                "SimpleMediaCollectionShow");

            migrationBuilder.DropForeignKey(
                "FK_SimpleMediaCollectionShow_TelevisionShow_TelevisionShowsId",
                "SimpleMediaCollectionShow");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionEpisode_MediaItem_Id",
                "TelevisionEpisode");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionEpisode_TelevisionSeason_SeasonId",
                "TelevisionEpisode");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionEpisodeMetadata_TelevisionEpisode_TelevisionEpisodeId",
                "TelevisionEpisodeMetadata");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionSeason_TelevisionShow_TelevisionShowId",
                "TelevisionSeason");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionShowMetadata_TelevisionShow_TelevisionShowId",
                "TelevisionShowMetadata");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionShowSource_TelevisionShow_TelevisionShowId",
                "TelevisionShowSource");

            migrationBuilder.DropPrimaryKey(
                "PK_TelevisionShow",
                "TelevisionShow");

            migrationBuilder.DropPrimaryKey(
                "PK_TelevisionSeason",
                "TelevisionSeason");

            migrationBuilder.DropPrimaryKey(
                "PK_TelevisionEpisode",
                "TelevisionEpisode");

            migrationBuilder.DropPrimaryKey(
                "PK_SimpleMediaCollectionShow",
                "SimpleMediaCollectionShow");

            migrationBuilder.DropPrimaryKey(
                "PK_SimpleMediaCollectionSeason",
                "SimpleMediaCollectionSeason");

            migrationBuilder.DropPrimaryKey(
                "PK_SimpleMediaCollectionMovie",
                "SimpleMediaCollectionMovie");

            migrationBuilder.DropPrimaryKey(
                "PK_SimpleMediaCollectionEpisode",
                "SimpleMediaCollectionEpisode");

            migrationBuilder.DropPrimaryKey(
                "PK_SimpleMediaCollection",
                "SimpleMediaCollection");

            migrationBuilder.DropPrimaryKey(
                "PK_Resolution",
                "Resolution");

            migrationBuilder.DropPrimaryKey(
                "PK_ProgramScheduleOneItem",
                "ProgramScheduleOneItem");

            migrationBuilder.DropPrimaryKey(
                "PK_ProgramScheduleMultipleItem",
                "ProgramScheduleMultipleItem");

            migrationBuilder.DropPrimaryKey(
                "PK_ProgramScheduleItem",
                "ProgramScheduleItem");

            migrationBuilder.DropPrimaryKey(
                "PK_ProgramScheduleFloodItem",
                "ProgramScheduleFloodItem");

            migrationBuilder.DropPrimaryKey(
                "PK_ProgramScheduleDurationItem",
                "ProgramScheduleDurationItem");

            migrationBuilder.DropPrimaryKey(
                "PK_ProgramSchedule",
                "ProgramSchedule");

            migrationBuilder.DropPrimaryKey(
                "PK_PlexMovie",
                "PlexMovie");

            migrationBuilder.DropPrimaryKey(
                "PK_PlayoutProgramScheduleAnchor",
                "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropPrimaryKey(
                "PK_PlayoutItem",
                "PlayoutItem");

            migrationBuilder.DropPrimaryKey(
                "PK_Playout",
                "Playout");

            migrationBuilder.DropPrimaryKey(
                "PK_Movie",
                "Movie");

            migrationBuilder.DropPrimaryKey(
                "PK_MediaSource",
                "MediaSource");

            migrationBuilder.DropPrimaryKey(
                "PK_MediaItem",
                "MediaItem");

            migrationBuilder.DropPrimaryKey(
                "PK_MediaCollection",
                "MediaCollection");

            migrationBuilder.DropPrimaryKey(
                "PK_FFmpegProfile",
                "FFmpegProfile");

            migrationBuilder.DropPrimaryKey(
                "PK_ConfigElement",
                "ConfigElement");

            migrationBuilder.DropPrimaryKey(
                "PK_Channel",
                "Channel");

            migrationBuilder.RenameTable(
                "TelevisionShow",
                newName: "TelevisionShows");

            migrationBuilder.RenameTable(
                "TelevisionSeason",
                newName: "TelevisionSeasons");

            migrationBuilder.RenameTable(
                "TelevisionEpisode",
                newName: "TelevisionEpisodes");

            migrationBuilder.RenameTable(
                "SimpleMediaCollectionShow",
                newName: "SimpleMediaCollectionShows");

            migrationBuilder.RenameTable(
                "SimpleMediaCollectionSeason",
                newName: "SimpleMediaCollectionSeasons");

            migrationBuilder.RenameTable(
                "SimpleMediaCollectionMovie",
                newName: "SimpleMediaCollectionMovies");

            migrationBuilder.RenameTable(
                "SimpleMediaCollectionEpisode",
                newName: "SimpleMediaCollectionEpisodes");

            migrationBuilder.RenameTable(
                "SimpleMediaCollection",
                newName: "SimpleMediaCollections");

            migrationBuilder.RenameTable(
                "Resolution",
                newName: "Resolutions");

            migrationBuilder.RenameTable(
                "ProgramScheduleOneItem",
                newName: "ProgramScheduleOneItems");

            migrationBuilder.RenameTable(
                "ProgramScheduleMultipleItem",
                newName: "ProgramScheduleMultipleItems");

            migrationBuilder.RenameTable(
                "ProgramScheduleItem",
                newName: "ProgramScheduleItems");

            migrationBuilder.RenameTable(
                "ProgramScheduleFloodItem",
                newName: "ProgramScheduleFloodItems");

            migrationBuilder.RenameTable(
                "ProgramScheduleDurationItem",
                newName: "ProgramScheduleDurationItems");

            migrationBuilder.RenameTable(
                "ProgramSchedule",
                newName: "ProgramSchedules");

            migrationBuilder.RenameTable(
                "PlexMovie",
                newName: "PlexMovies");

            migrationBuilder.RenameTable(
                "PlayoutProgramScheduleAnchor",
                newName: "PlayoutProgramScheduleItemAnchors");

            migrationBuilder.RenameTable(
                "PlayoutItem",
                newName: "PlayoutItems");

            migrationBuilder.RenameTable(
                "Playout",
                newName: "Playouts");

            migrationBuilder.RenameTable(
                "Movie",
                newName: "Movies");

            migrationBuilder.RenameTable(
                "MediaSource",
                newName: "MediaSources");

            migrationBuilder.RenameTable(
                "MediaItem",
                newName: "MediaItems");

            migrationBuilder.RenameTable(
                "MediaCollection",
                newName: "MediaCollections");

            migrationBuilder.RenameTable(
                "FFmpegProfile",
                newName: "FFmpegProfiles");

            migrationBuilder.RenameTable(
                "ConfigElement",
                newName: "ConfigElements");

            migrationBuilder.RenameTable(
                "Channel",
                newName: "Channels");

            migrationBuilder.RenameIndex(
                "IX_TelevisionSeason_TelevisionShowId",
                table: "TelevisionSeasons",
                newName: "IX_TelevisionSeasons_TelevisionShowId");

            migrationBuilder.RenameIndex(
                "IX_TelevisionEpisode_SeasonId",
                table: "TelevisionEpisodes",
                newName: "IX_TelevisionEpisodes_SeasonId");

            migrationBuilder.RenameIndex(
                "IX_SimpleMediaCollectionShow_TelevisionShowsId",
                table: "SimpleMediaCollectionShows",
                newName: "IX_SimpleMediaCollectionShows_TelevisionShowsId");

            migrationBuilder.RenameIndex(
                "IX_SimpleMediaCollectionSeason_TelevisionSeasonsId",
                table: "SimpleMediaCollectionSeasons",
                newName: "IX_SimpleMediaCollectionSeasons_TelevisionSeasonsId");

            migrationBuilder.RenameIndex(
                "IX_SimpleMediaCollectionMovie_SimpleMediaCollectionsId",
                table: "SimpleMediaCollectionMovies",
                newName: "IX_SimpleMediaCollectionMovies_SimpleMediaCollectionsId");

            migrationBuilder.RenameIndex(
                "IX_SimpleMediaCollectionEpisode_TelevisionEpisodesId",
                table: "SimpleMediaCollectionEpisodes",
                newName: "IX_SimpleMediaCollectionEpisodes_TelevisionEpisodesId");

            migrationBuilder.RenameIndex(
                "IX_ProgramScheduleItem_TelevisionShowId",
                table: "ProgramScheduleItems",
                newName: "IX_ProgramScheduleItems_TelevisionShowId");

            migrationBuilder.RenameIndex(
                "IX_ProgramScheduleItem_TelevisionSeasonId",
                table: "ProgramScheduleItems",
                newName: "IX_ProgramScheduleItems_TelevisionSeasonId");

            migrationBuilder.RenameIndex(
                "IX_ProgramScheduleItem_ProgramScheduleId",
                table: "ProgramScheduleItems",
                newName: "IX_ProgramScheduleItems_ProgramScheduleId");

            migrationBuilder.RenameIndex(
                "IX_ProgramScheduleItem_MediaCollectionId",
                table: "ProgramScheduleItems",
                newName: "IX_ProgramScheduleItems_MediaCollectionId");

            migrationBuilder.RenameIndex(
                "IX_ProgramSchedule_Name",
                table: "ProgramSchedules",
                newName: "IX_ProgramSchedules_Name");

            migrationBuilder.RenameIndex(
                "IX_PlexMovie_PartId",
                table: "PlexMovies",
                newName: "IX_PlexMovies_PartId");

            migrationBuilder.RenameIndex(
                "IX_PlayoutProgramScheduleAnchor_ProgramScheduleId",
                table: "PlayoutProgramScheduleItemAnchors",
                newName: "IX_PlayoutProgramScheduleItemAnchors_ProgramScheduleId");

            migrationBuilder.RenameIndex(
                "IX_PlayoutProgramScheduleAnchor_PlayoutId",
                table: "PlayoutProgramScheduleItemAnchors",
                newName: "IX_PlayoutProgramScheduleItemAnchors_PlayoutId");

            migrationBuilder.RenameIndex(
                "IX_PlayoutItem_PlayoutId",
                table: "PlayoutItems",
                newName: "IX_PlayoutItems_PlayoutId");

            migrationBuilder.RenameIndex(
                "IX_PlayoutItem_MediaItemId",
                table: "PlayoutItems",
                newName: "IX_PlayoutItems_MediaItemId");

            migrationBuilder.RenameIndex(
                "IX_Playout_ProgramScheduleId",
                table: "Playouts",
                newName: "IX_Playouts_ProgramScheduleId");

            migrationBuilder.RenameIndex(
                "IX_Playout_ChannelId",
                table: "Playouts",
                newName: "IX_Playouts_ChannelId");

            migrationBuilder.RenameIndex(
                "IX_Playout_Anchor_NextScheduleItemId",
                table: "Playouts",
                newName: "IX_Playouts_Anchor_NextScheduleItemId");

            migrationBuilder.RenameIndex(
                "IX_MediaItem_LibraryPathId",
                table: "MediaItems",
                newName: "IX_MediaItems_LibraryPathId");

            migrationBuilder.RenameIndex(
                "IX_MediaCollection_Name",
                table: "MediaCollections",
                newName: "IX_MediaCollections_Name");

            migrationBuilder.RenameIndex(
                "IX_FFmpegProfile_ResolutionId",
                table: "FFmpegProfiles",
                newName: "IX_FFmpegProfiles_ResolutionId");

            migrationBuilder.RenameIndex(
                "IX_ConfigElement_Key",
                table: "ConfigElements",
                newName: "IX_ConfigElements_Key");

            migrationBuilder.RenameIndex(
                "IX_Channel_Number",
                table: "Channels",
                newName: "IX_Channels_Number");

            migrationBuilder.RenameIndex(
                "IX_Channel_FFmpegProfileId",
                table: "Channels",
                newName: "IX_Channels_FFmpegProfileId");

            migrationBuilder.AddColumn<int>(
                "MovieId1",
                "MovieMetadata",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                "PK_TelevisionShows",
                "TelevisionShows",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_TelevisionSeasons",
                "TelevisionSeasons",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_TelevisionEpisodes",
                "TelevisionEpisodes",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_SimpleMediaCollectionShows",
                "SimpleMediaCollectionShows",
                new[] { "SimpleMediaCollectionsId", "TelevisionShowsId" });

            migrationBuilder.AddPrimaryKey(
                "PK_SimpleMediaCollectionSeasons",
                "SimpleMediaCollectionSeasons",
                new[] { "SimpleMediaCollectionsId", "TelevisionSeasonsId" });

            migrationBuilder.AddPrimaryKey(
                "PK_SimpleMediaCollectionMovies",
                "SimpleMediaCollectionMovies",
                new[] { "MoviesId", "SimpleMediaCollectionsId" });

            migrationBuilder.AddPrimaryKey(
                "PK_SimpleMediaCollectionEpisodes",
                "SimpleMediaCollectionEpisodes",
                new[] { "SimpleMediaCollectionsId", "TelevisionEpisodesId" });

            migrationBuilder.AddPrimaryKey(
                "PK_SimpleMediaCollections",
                "SimpleMediaCollections",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_Resolutions",
                "Resolutions",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ProgramScheduleOneItems",
                "ProgramScheduleOneItems",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ProgramScheduleMultipleItems",
                "ProgramScheduleMultipleItems",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ProgramScheduleItems",
                "ProgramScheduleItems",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ProgramScheduleFloodItems",
                "ProgramScheduleFloodItems",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ProgramScheduleDurationItems",
                "ProgramScheduleDurationItems",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ProgramSchedules",
                "ProgramSchedules",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_PlexMovies",
                "PlexMovies",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_PlayoutProgramScheduleItemAnchors",
                "PlayoutProgramScheduleItemAnchors",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_PlayoutItems",
                "PlayoutItems",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_Playouts",
                "Playouts",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_Movies",
                "Movies",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_MediaSources",
                "MediaSources",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_MediaItems",
                "MediaItems",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_MediaCollections",
                "MediaCollections",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_FFmpegProfiles",
                "FFmpegProfiles",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_ConfigElements",
                "ConfigElements",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_Channels",
                "Channels",
                "Id");

            migrationBuilder.CreateIndex(
                "IX_MovieMetadata_MovieId1",
                "MovieMetadata",
                "MovieId1");

            migrationBuilder.AddForeignKey(
                "FK_Channels_FFmpegProfiles_FFmpegProfileId",
                "Channels",
                "FFmpegProfileId",
                "FFmpegProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_FFmpegProfiles_Resolutions_ResolutionId",
                "FFmpegProfiles",
                "ResolutionId",
                "Resolutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Library_MediaSources_MediaSourceId",
                "Library",
                "MediaSourceId",
                "MediaSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_LocalMediaSource_MediaSources_Id",
                "LocalMediaSource",
                "Id",
                "MediaSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_MediaItems_LibraryPath_LibraryPathId",
                "MediaItems",
                "LibraryPathId",
                "LibraryPath",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_MovieMetadata_Movies_MovieId",
                "MovieMetadata",
                "MovieId",
                "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_MovieMetadata_Movies_MovieId1",
                "MovieMetadata",
                "MovieId1",
                "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Movies_MediaItems_Id",
                "Movies",
                "Id",
                "MediaItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutItems_MediaItems_MediaItemId",
                "PlayoutItems",
                "MediaItemId",
                "MediaItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutItems_Playouts_PlayoutId",
                "PlayoutItems",
                "PlayoutId",
                "Playouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutProgramScheduleItemAnchors_Playouts_PlayoutId",
                "PlayoutProgramScheduleItemAnchors",
                "PlayoutId",
                "Playouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutProgramScheduleItemAnchors_ProgramSchedules_ProgramScheduleId",
                "PlayoutProgramScheduleItemAnchors",
                "ProgramScheduleId",
                "ProgramSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Playouts_Channels_ChannelId",
                "Playouts",
                "ChannelId",
                "Channels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Playouts_ProgramScheduleItems_Anchor_NextScheduleItemId",
                "Playouts",
                "Anchor_NextScheduleItemId",
                "ProgramScheduleItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Playouts_ProgramSchedules_ProgramScheduleId",
                "Playouts",
                "ProgramScheduleId",
                "ProgramSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlexMediaSource_MediaSources_Id",
                "PlexMediaSource",
                "Id",
                "MediaSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlexMovies_Movies_Id",
                "PlexMovies",
                "Id",
                "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlexMovies_PlexMediaItemPart_PartId",
                "PlexMovies",
                "PartId",
                "PlexMediaItemPart",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleDurationItems_ProgramScheduleItems_Id",
                "ProgramScheduleDurationItems",
                "Id",
                "ProgramScheduleItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleFloodItems_ProgramScheduleItems_Id",
                "ProgramScheduleFloodItems",
                "Id",
                "ProgramScheduleItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItems_MediaCollections_MediaCollectionId",
                "ProgramScheduleItems",
                "MediaCollectionId",
                "MediaCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItems_ProgramSchedules_ProgramScheduleId",
                "ProgramScheduleItems",
                "ProgramScheduleId",
                "ProgramSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItems_TelevisionSeasons_TelevisionSeasonId",
                "ProgramScheduleItems",
                "TelevisionSeasonId",
                "TelevisionSeasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItems_TelevisionShows_TelevisionShowId",
                "ProgramScheduleItems",
                "TelevisionShowId",
                "TelevisionShows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleMultipleItems_ProgramScheduleItems_Id",
                "ProgramScheduleMultipleItems",
                "Id",
                "ProgramScheduleItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleOneItems_ProgramScheduleItems_Id",
                "ProgramScheduleOneItems",
                "Id",
                "ProgramScheduleItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionEpisodes_SimpleMediaCollections_SimpleMediaCollectionsId",
                "SimpleMediaCollectionEpisodes",
                "SimpleMediaCollectionsId",
                "SimpleMediaCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionEpisodes_TelevisionEpisodes_TelevisionEpisodesId",
                "SimpleMediaCollectionEpisodes",
                "TelevisionEpisodesId",
                "TelevisionEpisodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionMovies_Movies_MoviesId",
                "SimpleMediaCollectionMovies",
                "MoviesId",
                "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionMovies_SimpleMediaCollections_SimpleMediaCollectionsId",
                "SimpleMediaCollectionMovies",
                "SimpleMediaCollectionsId",
                "SimpleMediaCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollections_MediaCollections_Id",
                "SimpleMediaCollections",
                "Id",
                "MediaCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionSeasons_SimpleMediaCollections_SimpleMediaCollectionsId",
                "SimpleMediaCollectionSeasons",
                "SimpleMediaCollectionsId",
                "SimpleMediaCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionSeasons_TelevisionSeasons_TelevisionSeasonsId",
                "SimpleMediaCollectionSeasons",
                "TelevisionSeasonsId",
                "TelevisionSeasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionShows_SimpleMediaCollections_SimpleMediaCollectionsId",
                "SimpleMediaCollectionShows",
                "SimpleMediaCollectionsId",
                "SimpleMediaCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_SimpleMediaCollectionShows_TelevisionShows_TelevisionShowsId",
                "SimpleMediaCollectionShows",
                "TelevisionShowsId",
                "TelevisionShows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionEpisodeMetadata_TelevisionEpisodes_TelevisionEpisodeId",
                "TelevisionEpisodeMetadata",
                "TelevisionEpisodeId",
                "TelevisionEpisodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionEpisodes_MediaItems_Id",
                "TelevisionEpisodes",
                "Id",
                "MediaItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionEpisodes_TelevisionSeasons_SeasonId",
                "TelevisionEpisodes",
                "SeasonId",
                "TelevisionSeasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionSeasons_TelevisionShows_TelevisionShowId",
                "TelevisionSeasons",
                "TelevisionShowId",
                "TelevisionShows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionShowMetadata_TelevisionShows_TelevisionShowId",
                "TelevisionShowMetadata",
                "TelevisionShowId",
                "TelevisionShows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionShowSource_TelevisionShows_TelevisionShowId",
                "TelevisionShowSource",
                "TelevisionShowId",
                "TelevisionShows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
