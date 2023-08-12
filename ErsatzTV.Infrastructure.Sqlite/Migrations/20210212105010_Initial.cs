using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "ConfigElements",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>("TEXT", nullable: true),
                    Value = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_ConfigElements", x => x.Id); });

            migrationBuilder.CreateTable(
                "GenericIntegerIds",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table => { });

            migrationBuilder.CreateTable(
                "MediaCollections",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_MediaCollections", x => x.Id); });

            migrationBuilder.CreateTable(
                "MediaCollectionSummaries",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false),
                    Name = table.Column<string>("TEXT", nullable: true),
                    ItemCount = table.Column<int>("INTEGER", nullable: false),
                    IsSimple = table.Column<bool>("INTEGER", nullable: false)
                },
                constraints: table => { });

            migrationBuilder.CreateTable(
                "MediaSources",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceType = table.Column<int>("INTEGER", nullable: false),
                    Name = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_MediaSources", x => x.Id); });

            migrationBuilder.CreateTable(
                "ProgramSchedules",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true),
                    MediaCollectionPlaybackOrder = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_ProgramSchedules", x => x.Id); });

            migrationBuilder.CreateTable(
                "Resolutions",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true),
                    Height = table.Column<int>("INTEGER", nullable: false),
                    Width = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_Resolutions", x => x.Id); });

            migrationBuilder.CreateTable(
                "SimpleMediaCollections",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimpleMediaCollections", x => x.Id);
                    table.ForeignKey(
                        "FK_SimpleMediaCollections_MediaCollections_Id",
                        x => x.Id,
                        "MediaCollections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionMediaCollections",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShowTitle = table.Column<string>("TEXT", nullable: true),
                    SeasonNumber = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelevisionMediaCollections", x => x.Id);
                    table.ForeignKey(
                        "FK_TelevisionMediaCollections_MediaCollections_Id",
                        x => x.Id,
                        "MediaCollections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "LocalMediaSources",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaType = table.Column<int>("INTEGER", nullable: false),
                    Folder = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalMediaSources", x => x.Id);
                    table.ForeignKey(
                        "FK_LocalMediaSources_MediaSources_Id",
                        x => x.Id,
                        "MediaSources",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "MediaItems",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaSourceId = table.Column<int>("INTEGER", nullable: false),
                    Path = table.Column<string>("TEXT", nullable: true),
                    Metadata_Duration = table.Column<TimeSpan>("TEXT", nullable: true),
                    Metadata_SampleAspectRatio = table.Column<string>("TEXT", nullable: true),
                    Metadata_DisplayAspectRatio = table.Column<string>("TEXT", nullable: true),
                    Metadata_VideoCodec = table.Column<string>("TEXT", nullable: true),
                    Metadata_AudioCodec = table.Column<string>("TEXT", nullable: true),
                    Metadata_MediaType = table.Column<int>("INTEGER", nullable: true),
                    Metadata_Title = table.Column<string>("TEXT", nullable: true),
                    Metadata_Subtitle = table.Column<string>("TEXT", nullable: true),
                    Metadata_Description = table.Column<string>("TEXT", nullable: true),
                    Metadata_SeasonNumber = table.Column<int>("INTEGER", nullable: true),
                    Metadata_EpisodeNumber = table.Column<int>("INTEGER", nullable: true),
                    Metadata_ContentRating = table.Column<string>("TEXT", nullable: true),
                    Metadata_Aired = table.Column<DateTime>("TEXT", nullable: true),
                    Metadata_VideoScanType = table.Column<int>("INTEGER", nullable: true),
                    Metadata_Width = table.Column<int>("INTEGER", nullable: true),
                    Metadata_Height = table.Column<int>("INTEGER", nullable: true),
                    LastWriteTime = table.Column<DateTime>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItems", x => x.Id);
                    table.ForeignKey(
                        "FK_MediaItems_MediaSources_MediaSourceId",
                        x => x.MediaSourceId,
                        "MediaSources",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "PlexMediaSources",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductVersion = table.Column<string>("TEXT", nullable: true),
                    ClientIdentifier = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexMediaSources", x => x.Id);
                    table.ForeignKey(
                        "FK_PlexMediaSources_MediaSources_Id",
                        x => x.Id,
                        "MediaSources",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "ProgramScheduleItems",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Index = table.Column<int>("INTEGER", nullable: false),
                    StartTime = table.Column<TimeSpan>("TEXT", nullable: true),
                    MediaCollectionId = table.Column<int>("INTEGER", nullable: false),
                    ProgramScheduleId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleItems", x => x.Id);
                    table.ForeignKey(
                        "FK_ProgramScheduleItems_MediaCollections_MediaCollectionId",
                        x => x.MediaCollectionId,
                        "MediaCollections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_ProgramScheduleItems_ProgramSchedules_ProgramScheduleId",
                        x => x.ProgramScheduleId,
                        "ProgramSchedules",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "FFmpegProfiles",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true),
                    ThreadCount = table.Column<int>("INTEGER", nullable: false),
                    Transcode = table.Column<bool>("INTEGER", nullable: false),
                    ResolutionId = table.Column<int>("INTEGER", nullable: false),
                    NormalizeResolution = table.Column<bool>("INTEGER", nullable: false),
                    VideoCodec = table.Column<string>("TEXT", nullable: true),
                    NormalizeVideoCodec = table.Column<bool>("INTEGER", nullable: false),
                    VideoBitrate = table.Column<int>("INTEGER", nullable: false),
                    VideoBufferSize = table.Column<int>("INTEGER", nullable: false),
                    AudioCodec = table.Column<string>("TEXT", nullable: true),
                    NormalizeAudioCodec = table.Column<bool>("INTEGER", nullable: false),
                    AudioBitrate = table.Column<int>("INTEGER", nullable: false),
                    AudioBufferSize = table.Column<int>("INTEGER", nullable: false),
                    AudioVolume = table.Column<int>("INTEGER", nullable: false),
                    AudioChannels = table.Column<int>("INTEGER", nullable: false),
                    AudioSampleRate = table.Column<int>("INTEGER", nullable: false),
                    NormalizeAudio = table.Column<bool>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FFmpegProfiles", x => x.Id);
                    table.ForeignKey(
                        "FK_FFmpegProfiles_Resolutions_ResolutionId",
                        x => x.ResolutionId,
                        "Resolutions",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "MediaItemSimpleMediaCollection",
                table => new
                {
                    ItemsId = table.Column<int>("INTEGER", nullable: false),
                    SimpleMediaCollectionsId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_MediaItemSimpleMediaCollection",
                        x => new { x.ItemsId, x.SimpleMediaCollectionsId });
                    table.ForeignKey(
                        "FK_MediaItemSimpleMediaCollection_MediaItems_ItemsId",
                        x => x.ItemsId,
                        "MediaItems",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_MediaItemSimpleMediaCollection_SimpleMediaCollections_SimpleMediaCollectionsId",
                        x => x.SimpleMediaCollectionsId,
                        "SimpleMediaCollections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "PlexMediaSourceConnections",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>("INTEGER", nullable: false),
                    Uri = table.Column<string>("TEXT", nullable: true),
                    PlexMediaSourceId = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexMediaSourceConnections", x => x.Id);
                    table.ForeignKey(
                        "FK_PlexMediaSourceConnections_PlexMediaSources_PlexMediaSourceId",
                        x => x.PlexMediaSourceId,
                        "PlexMediaSources",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                "PlexMediaSourceLibraries",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>("TEXT", nullable: true),
                    Name = table.Column<string>("TEXT", nullable: true),
                    MediaType = table.Column<int>("INTEGER", nullable: false),
                    PlexMediaSourceId = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexMediaSourceLibraries", x => x.Id);
                    table.ForeignKey(
                        "FK_PlexMediaSourceLibraries_PlexMediaSources_PlexMediaSourceId",
                        x => x.PlexMediaSourceId,
                        "PlexMediaSources",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                "ProgramScheduleDurationItems",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayoutDuration = table.Column<TimeSpan>("TEXT", nullable: false),
                    OfflineTail = table.Column<bool>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleDurationItems", x => x.Id);
                    table.ForeignKey(
                        "FK_ProgramScheduleDurationItems_ProgramScheduleItems_Id",
                        x => x.Id,
                        "ProgramScheduleItems",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "ProgramScheduleFloodItems",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleFloodItems", x => x.Id);
                    table.ForeignKey(
                        "FK_ProgramScheduleFloodItems_ProgramScheduleItems_Id",
                        x => x.Id,
                        "ProgramScheduleItems",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "ProgramScheduleMultipleItems",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Count = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleMultipleItems", x => x.Id);
                    table.ForeignKey(
                        "FK_ProgramScheduleMultipleItems_ProgramScheduleItems_Id",
                        x => x.Id,
                        "ProgramScheduleItems",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "ProgramScheduleOneItems",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleOneItems", x => x.Id);
                    table.ForeignKey(
                        "FK_ProgramScheduleOneItems_ProgramScheduleItems_Id",
                        x => x.Id,
                        "ProgramScheduleItems",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "Channels",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UniqueId = table.Column<Guid>("TEXT", nullable: false),
                    Number = table.Column<int>("INTEGER", nullable: false),
                    Name = table.Column<string>("TEXT", nullable: true),
                    Logo = table.Column<string>("TEXT", nullable: true),
                    FFmpegProfileId = table.Column<int>("INTEGER", nullable: false),
                    StreamingMode = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                    table.ForeignKey(
                        "FK_Channels_FFmpegProfiles_FFmpegProfileId",
                        x => x.FFmpegProfileId,
                        "FFmpegProfiles",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "Playouts",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<int>("INTEGER", nullable: false),
                    ProgramScheduleId = table.Column<int>("INTEGER", nullable: false),
                    ProgramSchedulePlayoutType = table.Column<int>("INTEGER", nullable: false),
                    Anchor_NextScheduleItemId = table.Column<int>("INTEGER", nullable: true),
                    Anchor_NextStart = table.Column<DateTimeOffset>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playouts", x => x.Id);
                    table.ForeignKey(
                        "FK_Playouts_Channels_ChannelId",
                        x => x.ChannelId,
                        "Channels",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Playouts_ProgramScheduleItems_Anchor_NextScheduleItemId",
                        x => x.Anchor_NextScheduleItemId,
                        "ProgramScheduleItems",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Playouts_ProgramSchedules_ProgramScheduleId",
                        x => x.ProgramScheduleId,
                        "ProgramSchedules",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "PlayoutItems",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaItemId = table.Column<int>("INTEGER", nullable: false),
                    Start = table.Column<DateTimeOffset>("TEXT", nullable: false),
                    Finish = table.Column<DateTimeOffset>("TEXT", nullable: false),
                    PlayoutId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoutItems", x => x.Id);
                    table.ForeignKey(
                        "FK_PlayoutItems_MediaItems_MediaItemId",
                        x => x.MediaItemId,
                        "MediaItems",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_PlayoutItems_Playouts_PlayoutId",
                        x => x.PlayoutId,
                        "Playouts",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "PlayoutProgramScheduleItemAnchors",
                table => new
                {
                    PlayoutId = table.Column<int>("INTEGER", nullable: false),
                    ProgramScheduleId = table.Column<int>("INTEGER", nullable: false),
                    MediaCollectionId = table.Column<int>("INTEGER", nullable: false),
                    EnumeratorState_Seed = table.Column<int>("INTEGER", nullable: true),
                    EnumeratorState_Index = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_PlayoutProgramScheduleItemAnchors",
                        x => new { x.PlayoutId, x.ProgramScheduleId, x.MediaCollectionId });
                    table.ForeignKey(
                        "FK_PlayoutProgramScheduleItemAnchors_MediaCollections_MediaCollectionId",
                        x => x.MediaCollectionId,
                        "MediaCollections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_PlayoutProgramScheduleItemAnchors_Playouts_PlayoutId",
                        x => x.PlayoutId,
                        "Playouts",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_PlayoutProgramScheduleItemAnchors_ProgramSchedules_ProgramScheduleId",
                        x => x.ProgramScheduleId,
                        "ProgramSchedules",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Channels_FFmpegProfileId",
                "Channels",
                "FFmpegProfileId");

            migrationBuilder.CreateIndex(
                "IX_Channels_Number",
                "Channels",
                "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_ConfigElements_Key",
                "ConfigElements",
                "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_FFmpegProfiles_ResolutionId",
                "FFmpegProfiles",
                "ResolutionId");

            migrationBuilder.CreateIndex(
                "IX_MediaCollections_Name",
                "MediaCollections",
                "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_MediaItems_MediaSourceId",
                "MediaItems",
                "MediaSourceId");

            migrationBuilder.CreateIndex(
                "IX_MediaItemSimpleMediaCollection_SimpleMediaCollectionsId",
                "MediaItemSimpleMediaCollection",
                "SimpleMediaCollectionsId");

            migrationBuilder.CreateIndex(
                "IX_MediaSources_Name",
                "MediaSources",
                "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_PlayoutItems_MediaItemId",
                "PlayoutItems",
                "MediaItemId");

            migrationBuilder.CreateIndex(
                "IX_PlayoutItems_PlayoutId",
                "PlayoutItems",
                "PlayoutId");

            migrationBuilder.CreateIndex(
                "IX_PlayoutProgramScheduleItemAnchors_MediaCollectionId",
                "PlayoutProgramScheduleItemAnchors",
                "MediaCollectionId");

            migrationBuilder.CreateIndex(
                "IX_PlayoutProgramScheduleItemAnchors_ProgramScheduleId",
                "PlayoutProgramScheduleItemAnchors",
                "ProgramScheduleId");

            migrationBuilder.CreateIndex(
                "IX_Playouts_Anchor_NextScheduleItemId",
                "Playouts",
                "Anchor_NextScheduleItemId");

            migrationBuilder.CreateIndex(
                "IX_Playouts_ChannelId",
                "Playouts",
                "ChannelId");

            migrationBuilder.CreateIndex(
                "IX_Playouts_ProgramScheduleId",
                "Playouts",
                "ProgramScheduleId");

            migrationBuilder.CreateIndex(
                "IX_PlexMediaSourceConnections_PlexMediaSourceId",
                "PlexMediaSourceConnections",
                "PlexMediaSourceId");

            migrationBuilder.CreateIndex(
                "IX_PlexMediaSourceLibraries_PlexMediaSourceId",
                "PlexMediaSourceLibraries",
                "PlexMediaSourceId");

            migrationBuilder.CreateIndex(
                "IX_ProgramScheduleItems_MediaCollectionId",
                "ProgramScheduleItems",
                "MediaCollectionId");

            migrationBuilder.CreateIndex(
                "IX_ProgramScheduleItems_ProgramScheduleId",
                "ProgramScheduleItems",
                "ProgramScheduleId");

            migrationBuilder.CreateIndex(
                "IX_ProgramSchedules_Name",
                "ProgramSchedules",
                "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_TelevisionMediaCollections_ShowTitle_SeasonNumber",
                "TelevisionMediaCollections",
                new[] { "ShowTitle", "SeasonNumber" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "ConfigElements");

            migrationBuilder.DropTable(
                "GenericIntegerIds");

            migrationBuilder.DropTable(
                "LocalMediaSources");

            migrationBuilder.DropTable(
                "MediaCollectionSummaries");

            migrationBuilder.DropTable(
                "MediaItemSimpleMediaCollection");

            migrationBuilder.DropTable(
                "PlayoutItems");

            migrationBuilder.DropTable(
                "PlayoutProgramScheduleItemAnchors");

            migrationBuilder.DropTable(
                "PlexMediaSourceConnections");

            migrationBuilder.DropTable(
                "PlexMediaSourceLibraries");

            migrationBuilder.DropTable(
                "ProgramScheduleDurationItems");

            migrationBuilder.DropTable(
                "ProgramScheduleFloodItems");

            migrationBuilder.DropTable(
                "ProgramScheduleMultipleItems");

            migrationBuilder.DropTable(
                "ProgramScheduleOneItems");

            migrationBuilder.DropTable(
                "TelevisionMediaCollections");

            migrationBuilder.DropTable(
                "SimpleMediaCollections");

            migrationBuilder.DropTable(
                "MediaItems");

            migrationBuilder.DropTable(
                "Playouts");

            migrationBuilder.DropTable(
                "PlexMediaSources");

            migrationBuilder.DropTable(
                "Channels");

            migrationBuilder.DropTable(
                "ProgramScheduleItems");

            migrationBuilder.DropTable(
                "MediaSources");

            migrationBuilder.DropTable(
                "FFmpegProfiles");

            migrationBuilder.DropTable(
                "MediaCollections");

            migrationBuilder.DropTable(
                "ProgramSchedules");

            migrationBuilder.DropTable(
                "Resolutions");
        }
    }
}
