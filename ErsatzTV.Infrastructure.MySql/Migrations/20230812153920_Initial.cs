using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ChannelWatermark",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    ImageSource = table.Column<int>(type: "int", nullable: false),
                    Image = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Location = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    WidthPercent = table.Column<int>(type: "int", nullable: false),
                    HorizontalMarginPercent = table.Column<int>(type: "int", nullable: false),
                    VerticalMarginPercent = table.Column<int>(type: "int", nullable: false),
                    FrequencyMinutes = table.Column<int>(type: "int", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    Opacity = table.Column<int>(type: "int", nullable: false),
                    PlaceWithinSourceContent = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelWatermark", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Collection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UseCustomPlaybackOrder = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collection", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConfigElement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Key = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigElement", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmbyCollection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyCollection", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JellyfinCollection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinCollection", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LanguageCode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ThreeCode1 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThreeCode2 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TwoCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnglishName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FrenchName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageCode", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MediaSource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaSource", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MultiCollection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiCollection", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProgramSchedule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KeepMultiPartEpisodesTogether = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TreatCollectionsAsShows = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShuffleScheduleItems = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RandomStartPoint = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramSchedule", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Resolution",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCustom = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resolution", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SmartCollection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Query = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartCollection", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TraktList",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TraktId = table.Column<int>(type: "int", nullable: false),
                    User = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    List = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraktList", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmbyMediaSource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ServerName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperatingSystem = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastCollectionsScan = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyMediaSource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmbyMediaSource_MediaSource_Id",
                        column: x => x.Id,
                        principalTable: "MediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JellyfinMediaSource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ServerName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperatingSystem = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinMediaSource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinMediaSource_MediaSource_Id",
                        column: x => x.Id,
                        principalTable: "MediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Library",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MediaKind = table.Column<int>(type: "int", nullable: false),
                    LastScan = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MediaSourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Library", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Library_MediaSource_MediaSourceId",
                        column: x => x.MediaSourceId,
                        principalTable: "MediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LocalMediaSource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalMediaSource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalMediaSource_MediaSource_Id",
                        column: x => x.Id,
                        principalTable: "MediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlexMediaSource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ServerName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductVersion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Platform = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlatformVersion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientIdentifier = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexMediaSource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlexMediaSource_MediaSource_Id",
                        column: x => x.Id,
                        principalTable: "MediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MultiCollectionItem",
                columns: table => new
                {
                    MultiCollectionId = table.Column<int>(type: "int", nullable: false),
                    CollectionId = table.Column<int>(type: "int", nullable: false),
                    ScheduleAsGroup = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PlaybackOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiCollectionItem", x => new { x.MultiCollectionId, x.CollectionId });
                    table.ForeignKey(
                        name: "FK_MultiCollectionItem_Collection_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MultiCollectionItem_MultiCollection_MultiCollectionId",
                        column: x => x.MultiCollectionId,
                        principalTable: "MultiCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FFmpegProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThreadCount = table.Column<int>(type: "int", nullable: false),
                    HardwareAcceleration = table.Column<int>(type: "int", nullable: false),
                    VaapiDriver = table.Column<int>(type: "int", nullable: false),
                    VaapiDevice = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QsvExtraHardwareFrames = table.Column<int>(type: "int", nullable: true),
                    ResolutionId = table.Column<int>(type: "int", nullable: false),
                    VideoFormat = table.Column<int>(type: "int", nullable: false),
                    BitDepth = table.Column<int>(type: "int", nullable: false),
                    VideoBitrate = table.Column<int>(type: "int", nullable: false),
                    VideoBufferSize = table.Column<int>(type: "int", nullable: false),
                    AudioFormat = table.Column<int>(type: "int", nullable: false),
                    AudioBitrate = table.Column<int>(type: "int", nullable: false),
                    AudioBufferSize = table.Column<int>(type: "int", nullable: false),
                    NormalizeLoudness = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AudioChannels = table.Column<int>(type: "int", nullable: false),
                    AudioSampleRate = table.Column<int>(type: "int", nullable: false),
                    NormalizeFramerate = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    DeinterlaceVideo = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FFmpegProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FFmpegProfile_Resolution_ResolutionId",
                        column: x => x.ResolutionId,
                        principalTable: "Resolution",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MultiCollectionSmartItem",
                columns: table => new
                {
                    MultiCollectionId = table.Column<int>(type: "int", nullable: false),
                    SmartCollectionId = table.Column<int>(type: "int", nullable: false),
                    ScheduleAsGroup = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PlaybackOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiCollectionSmartItem", x => new { x.MultiCollectionId, x.SmartCollectionId });
                    table.ForeignKey(
                        name: "FK_MultiCollectionSmartItem_MultiCollection_MultiCollectionId",
                        column: x => x.MultiCollectionId,
                        principalTable: "MultiCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MultiCollectionSmartItem_SmartCollection_SmartCollectionId",
                        column: x => x.SmartCollectionId,
                        principalTable: "SmartCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmbyConnection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Address = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmbyMediaSourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyConnection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmbyConnection_EmbyMediaSource_EmbyMediaSourceId",
                        column: x => x.EmbyMediaSourceId,
                        principalTable: "EmbyMediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmbyPathReplacement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmbyPath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LocalPath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmbyMediaSourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyPathReplacement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmbyPathReplacement_EmbyMediaSource_EmbyMediaSourceId",
                        column: x => x.EmbyMediaSourceId,
                        principalTable: "EmbyMediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JellyfinConnection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Address = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JellyfinMediaSourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinConnection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinConnection_JellyfinMediaSource_JellyfinMediaSourceId",
                        column: x => x.JellyfinMediaSourceId,
                        principalTable: "JellyfinMediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JellyfinPathReplacement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    JellyfinPath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LocalPath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JellyfinMediaSourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinPathReplacement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinPathReplacement_JellyfinMediaSource_JellyfinMediaSou~",
                        column: x => x.JellyfinMediaSourceId,
                        principalTable: "JellyfinMediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmbyLibrary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShouldSyncItems = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyLibrary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmbyLibrary_Library_Id",
                        column: x => x.Id,
                        principalTable: "Library",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JellyfinLibrary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShouldSyncItems = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinLibrary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinLibrary_Library_Id",
                        column: x => x.Id,
                        principalTable: "Library",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryPath",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Path = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastScan = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LibraryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryPath", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryPath_Library_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Library",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LocalLibrary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalLibrary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalLibrary_Library_Id",
                        column: x => x.Id,
                        principalTable: "Library",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlexLibrary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShouldSyncItems = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexLibrary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlexLibrary_Library_Id",
                        column: x => x.Id,
                        principalTable: "Library",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlexConnection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Uri = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlexMediaSourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexConnection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlexConnection_PlexMediaSource_PlexMediaSourceId",
                        column: x => x.PlexMediaSourceId,
                        principalTable: "PlexMediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlexPathReplacement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlexPath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LocalPath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlexMediaSourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexPathReplacement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlexPathReplacement_PlexMediaSource_PlexMediaSourceId",
                        column: x => x.PlexMediaSourceId,
                        principalTable: "PlexMediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmbyPathInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Path = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NetworkPath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmbyLibraryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyPathInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmbyPathInfo_EmbyLibrary_EmbyLibraryId",
                        column: x => x.EmbyLibraryId,
                        principalTable: "EmbyLibrary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JellyfinPathInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Path = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NetworkPath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JellyfinLibraryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinPathInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinPathInfo_JellyfinLibrary_JellyfinLibraryId",
                        column: x => x.JellyfinLibraryId,
                        principalTable: "JellyfinLibrary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibraryFolder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Path = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LibraryPathId = table.Column<int>(type: "int", nullable: false),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryFolder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryFolder_LibraryPath_LibraryPathId",
                        column: x => x.LibraryPathId,
                        principalTable: "LibraryPath",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MediaItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LibraryPathId = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaItem_LibraryPath_LibraryPathId",
                        column: x => x.LibraryPathId,
                        principalTable: "LibraryPath",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Artist",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Artist_MediaItem_Id",
                        column: x => x.Id,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CollectionItem",
                columns: table => new
                {
                    CollectionId = table.Column<int>(type: "int", nullable: false),
                    MediaItemId = table.Column<int>(type: "int", nullable: false),
                    CustomIndex = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionItem", x => new { x.CollectionId, x.MediaItemId });
                    table.ForeignKey(
                        name: "FK_CollectionItem_Collection_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionItem_MediaItem_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FillerPreset",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FillerKind = table.Column<int>(type: "int", nullable: false),
                    FillerMode = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    Count = table.Column<int>(type: "int", nullable: true),
                    PadToNearestMinute = table.Column<int>(type: "int", nullable: true),
                    AllowWatermarks = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CollectionType = table.Column<int>(type: "int", nullable: false),
                    CollectionId = table.Column<int>(type: "int", nullable: true),
                    MediaItemId = table.Column<int>(type: "int", nullable: true),
                    MultiCollectionId = table.Column<int>(type: "int", nullable: true),
                    SmartCollectionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FillerPreset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FillerPreset_Collection_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FillerPreset_MediaItem_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FillerPreset_MultiCollection_MultiCollectionId",
                        column: x => x.MultiCollectionId,
                        principalTable: "MultiCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FillerPreset_SmartCollection_SmartCollectionId",
                        column: x => x.SmartCollectionId,
                        principalTable: "SmartCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Movie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movie_MediaItem_Id",
                        column: x => x.Id,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OtherVideo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherVideo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherVideo_MediaItem_Id",
                        column: x => x.Id,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Show",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Show", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Show_MediaItem_Id",
                        column: x => x.Id,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Song",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Song", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Song_MediaItem_Id",
                        column: x => x.Id,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TraktListItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TraktListId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    TraktId = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: true),
                    Season = table.Column<int>(type: "int", nullable: true),
                    Episode = table.Column<int>(type: "int", nullable: true),
                    MediaItemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraktListItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraktListItem_MediaItem_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TraktListItem_TraktList_TraktListId",
                        column: x => x.TraktListId,
                        principalTable: "TraktList",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArtistMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Disambiguation = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Biography = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Formed = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistId = table.Column<int>(type: "int", nullable: false),
                    MetadataKind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtistMetadata_Artist_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artist",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MusicVideo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ArtistId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicVideo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicVideo_Artist_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artist",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MusicVideo_MediaItem_Id",
                        column: x => x.Id,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Channel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UniqueId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Number = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Group = table.Column<string>(type: "longtext", nullable: false, defaultValue: "ErsatzTV")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Categories = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FFmpegProfileId = table.Column<int>(type: "int", nullable: false),
                    WatermarkId = table.Column<int>(type: "int", nullable: true),
                    FallbackFillerId = table.Column<int>(type: "int", nullable: true),
                    StreamingMode = table.Column<int>(type: "int", nullable: false),
                    PreferredAudioLanguageCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferredAudioTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferredSubtitleLanguageCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubtitleMode = table.Column<int>(type: "int", nullable: false),
                    MusicVideoCreditsMode = table.Column<int>(type: "int", nullable: false),
                    MusicVideoCreditsTemplate = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Channel_ChannelWatermark_WatermarkId",
                        column: x => x.WatermarkId,
                        principalTable: "ChannelWatermark",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Channel_FFmpegProfile_FFmpegProfileId",
                        column: x => x.FFmpegProfileId,
                        principalTable: "FFmpegProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Channel_FillerPreset_FallbackFillerId",
                        column: x => x.FallbackFillerId,
                        principalTable: "FillerPreset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProgramScheduleItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    CollectionType = table.Column<int>(type: "int", nullable: false),
                    GuideMode = table.Column<int>(type: "int", nullable: false),
                    CustomTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProgramScheduleId = table.Column<int>(type: "int", nullable: false),
                    CollectionId = table.Column<int>(type: "int", nullable: true),
                    MediaItemId = table.Column<int>(type: "int", nullable: true),
                    MultiCollectionId = table.Column<int>(type: "int", nullable: true),
                    SmartCollectionId = table.Column<int>(type: "int", nullable: true),
                    PlaybackOrder = table.Column<int>(type: "int", nullable: false),
                    PreRollFillerId = table.Column<int>(type: "int", nullable: true),
                    MidRollFillerId = table.Column<int>(type: "int", nullable: true),
                    PostRollFillerId = table.Column<int>(type: "int", nullable: true),
                    TailFillerId = table.Column<int>(type: "int", nullable: true),
                    FallbackFillerId = table.Column<int>(type: "int", nullable: true),
                    WatermarkId = table.Column<int>(type: "int", nullable: true),
                    PreferredAudioLanguageCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferredAudioTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferredSubtitleLanguageCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubtitleMode = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItem_ChannelWatermark_WatermarkId",
                        column: x => x.WatermarkId,
                        principalTable: "ChannelWatermark",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItem_Collection_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItem_FillerPreset_FallbackFillerId",
                        column: x => x.FallbackFillerId,
                        principalTable: "FillerPreset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItem_FillerPreset_MidRollFillerId",
                        column: x => x.MidRollFillerId,
                        principalTable: "FillerPreset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItem_FillerPreset_PostRollFillerId",
                        column: x => x.PostRollFillerId,
                        principalTable: "FillerPreset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItem_FillerPreset_PreRollFillerId",
                        column: x => x.PreRollFillerId,
                        principalTable: "FillerPreset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItem_FillerPreset_TailFillerId",
                        column: x => x.TailFillerId,
                        principalTable: "FillerPreset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItem_MediaItem_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItem_MultiCollection_MultiCollectionId",
                        column: x => x.MultiCollectionId,
                        principalTable: "MultiCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItem_ProgramSchedule_ProgramScheduleId",
                        column: x => x.ProgramScheduleId,
                        principalTable: "ProgramSchedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItem_SmartCollection_SmartCollectionId",
                        column: x => x.SmartCollectionId,
                        principalTable: "SmartCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmbyMovie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyMovie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmbyMovie_Movie_Id",
                        column: x => x.Id,
                        principalTable: "Movie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JellyfinMovie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinMovie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinMovie_Movie_Id",
                        column: x => x.Id,
                        principalTable: "Movie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MovieMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ContentRating = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Outline = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Plot = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tagline = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MovieId = table.Column<int>(type: "int", nullable: false),
                    MetadataKind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovieMetadata_Movie_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlexMovie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexMovie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlexMovie_Movie_Id",
                        column: x => x.Id,
                        principalTable: "Movie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OtherVideoMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ContentRating = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Outline = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Plot = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tagline = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OtherVideoId = table.Column<int>(type: "int", nullable: false),
                    MetadataKind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherVideoMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherVideoMetadata_OtherVideo_OtherVideoId",
                        column: x => x.OtherVideoId,
                        principalTable: "OtherVideo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmbyShow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyShow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmbyShow_Show_Id",
                        column: x => x.Id,
                        principalTable: "Show",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JellyfinShow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinShow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinShow_Show_Id",
                        column: x => x.Id,
                        principalTable: "Show",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlexShow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexShow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlexShow_Show_Id",
                        column: x => x.Id,
                        principalTable: "Show",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Season",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    SeasonNumber = table.Column<int>(type: "int", nullable: false),
                    ShowId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Season", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Season_MediaItem_Id",
                        column: x => x.Id,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Season_Show_ShowId",
                        column: x => x.ShowId,
                        principalTable: "Show",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShowMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ContentRating = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Outline = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Plot = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tagline = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShowId = table.Column<int>(type: "int", nullable: false),
                    MetadataKind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShowMetadata_Show_ShowId",
                        column: x => x.ShowId,
                        principalTable: "Show",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SongMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Album = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Artist = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlbumArtist = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Track = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SongId = table.Column<int>(type: "int", nullable: false),
                    MetadataKind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongMetadata_Song_SongId",
                        column: x => x.SongId,
                        principalTable: "Song",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TraktListItemGuid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TraktListItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraktListItemGuid", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraktListItemGuid_TraktListItem_TraktListItemId",
                        column: x => x.TraktListItemId,
                        principalTable: "TraktListItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Mood",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistMetadataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mood", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mood_ArtistMetadata_ArtistMetadataId",
                        column: x => x.ArtistMetadataId,
                        principalTable: "ArtistMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Style",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistMetadataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Style", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Style_ArtistMetadata_ArtistMetadataId",
                        column: x => x.ArtistMetadataId,
                        principalTable: "ArtistMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MusicVideoMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Album = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Plot = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Track = table.Column<int>(type: "int", nullable: true),
                    MusicVideoId = table.Column<int>(type: "int", nullable: false),
                    MetadataKind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicVideoMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicVideoMetadata_MusicVideo_MusicVideoId",
                        column: x => x.MusicVideoId,
                        principalTable: "MusicVideo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Playout",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    ProgramScheduleId = table.Column<int>(type: "int", nullable: false),
                    ProgramSchedulePlayoutType = table.Column<int>(type: "int", nullable: false),
                    DailyRebuildTime = table.Column<TimeSpan>(type: "time(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playout", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Playout_Channel_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Playout_ProgramSchedule_ProgramScheduleId",
                        column: x => x.ProgramScheduleId,
                        principalTable: "ProgramSchedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProgramScheduleDurationItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    PlayoutDuration = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    TailMode = table.Column<int>(type: "int", nullable: false),
                    DiscardToFillAttempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleDurationItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleDurationItem_ProgramScheduleItem_Id",
                        column: x => x.Id,
                        principalTable: "ProgramScheduleItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProgramScheduleFloodItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleFloodItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleFloodItem_ProgramScheduleItem_Id",
                        column: x => x.Id,
                        principalTable: "ProgramScheduleItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProgramScheduleMultipleItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleMultipleItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleMultipleItem_ProgramScheduleItem_Id",
                        column: x => x.Id,
                        principalTable: "ProgramScheduleItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProgramScheduleOneItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleOneItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleOneItem_ProgramScheduleItem_Id",
                        column: x => x.Id,
                        principalTable: "ProgramScheduleItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmbySeason",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbySeason", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmbySeason_Season_Id",
                        column: x => x.Id,
                        principalTable: "Season",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Episode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    SeasonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Episode_MediaItem_Id",
                        column: x => x.Id,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Episode_Season_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Season",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JellyfinSeason",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinSeason", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinSeason_Season_Id",
                        column: x => x.Id,
                        principalTable: "Season",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlexSeason",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexSeason", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlexSeason_Season_Id",
                        column: x => x.Id,
                        principalTable: "Season",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SeasonMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Outline = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SeasonId = table.Column<int>(type: "int", nullable: false),
                    MetadataKind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonMetadata_Season_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Season",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MusicVideoArtist",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MusicVideoMetadataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicVideoArtist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicVideoArtist_MusicVideoMetadata_MusicVideoMetadataId",
                        column: x => x.MusicVideoMetadataId,
                        principalTable: "MusicVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayoutAnchor",
                columns: table => new
                {
                    PlayoutId = table.Column<int>(type: "int", nullable: false),
                    NextStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MultipleRemaining = table.Column<int>(type: "int", nullable: true),
                    DurationFinish = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    InFlood = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    InDurationFiller = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NextGuideGroup = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoutAnchor", x => x.PlayoutId);
                    table.ForeignKey(
                        name: "FK_PlayoutAnchor_Playout_PlayoutId",
                        column: x => x.PlayoutId,
                        principalTable: "Playout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayoutItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MediaItemId = table.Column<int>(type: "int", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Finish = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    GuideFinish = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CustomTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GuideGroup = table.Column<int>(type: "int", nullable: false),
                    FillerKind = table.Column<int>(type: "int", nullable: false),
                    PlayoutId = table.Column<int>(type: "int", nullable: false),
                    InPoint = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    OutPoint = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    ChapterTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WatermarkId = table.Column<int>(type: "int", nullable: true),
                    DisableWatermarks = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PreferredAudioLanguageCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferredAudioTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferredSubtitleLanguageCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubtitleMode = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoutItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoutItem_ChannelWatermark_WatermarkId",
                        column: x => x.WatermarkId,
                        principalTable: "ChannelWatermark",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlayoutItem_MediaItem_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoutItem_Playout_PlayoutId",
                        column: x => x.PlayoutId,
                        principalTable: "Playout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayoutProgramScheduleAnchor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayoutId = table.Column<int>(type: "int", nullable: false),
                    AnchorDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CollectionType = table.Column<int>(type: "int", nullable: false),
                    CollectionId = table.Column<int>(type: "int", nullable: true),
                    MultiCollectionId = table.Column<int>(type: "int", nullable: true),
                    SmartCollectionId = table.Column<int>(type: "int", nullable: true),
                    MediaItemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoutProgramScheduleAnchor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoutProgramScheduleAnchor_Collection_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoutProgramScheduleAnchor_MediaItem_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoutProgramScheduleAnchor_MultiCollection_MultiCollection~",
                        column: x => x.MultiCollectionId,
                        principalTable: "MultiCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoutProgramScheduleAnchor_Playout_PlayoutId",
                        column: x => x.PlayoutId,
                        principalTable: "Playout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoutProgramScheduleAnchor_SmartCollection_SmartCollection~",
                        column: x => x.SmartCollectionId,
                        principalTable: "SmartCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProgramScheduleAlternate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayoutId = table.Column<int>(type: "int", nullable: false),
                    ProgramScheduleId = table.Column<int>(type: "int", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    DaysOfWeek = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DaysOfMonth = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MonthsOfYear = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleAlternate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleAlternate_Playout_PlayoutId",
                        column: x => x.PlayoutId,
                        principalTable: "Playout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleAlternate_ProgramSchedule_ProgramScheduleId",
                        column: x => x.ProgramScheduleId,
                        principalTable: "ProgramSchedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmbyEpisode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyEpisode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmbyEpisode_Episode_Id",
                        column: x => x.Id,
                        principalTable: "Episode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EpisodeMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EpisodeNumber = table.Column<int>(type: "int", nullable: false),
                    Outline = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Plot = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tagline = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EpisodeId = table.Column<int>(type: "int", nullable: false),
                    MetadataKind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodeMetadata_Episode_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JellyfinEpisode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinEpisode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinEpisode_Episode_Id",
                        column: x => x.Id,
                        principalTable: "Episode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MediaVersion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Duration = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    SampleAspectRatio = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayAspectRatio = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RFrameRate = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VideoScanKind = table.Column<int>(type: "int", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    EpisodeId = table.Column<int>(type: "int", nullable: true),
                    MovieId = table.Column<int>(type: "int", nullable: true),
                    MusicVideoId = table.Column<int>(type: "int", nullable: true),
                    OtherVideoId = table.Column<int>(type: "int", nullable: true),
                    SongId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaVersion_Episode_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaVersion_Movie_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaVersion_MusicVideo_MusicVideoId",
                        column: x => x.MusicVideoId,
                        principalTable: "MusicVideo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaVersion_OtherVideo_OtherVideoId",
                        column: x => x.OtherVideoId,
                        principalTable: "OtherVideo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaVersion_Song_SongId",
                        column: x => x.SongId,
                        principalTable: "Song",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlexEpisode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Etag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexEpisode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlexEpisode_Episode_Id",
                        column: x => x.Id,
                        principalTable: "Episode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ScheduleItemsEnumeratorState",
                columns: table => new
                {
                    PlayoutAnchorPlayoutId = table.Column<int>(type: "int", nullable: false),
                    Seed = table.Column<int>(type: "int", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleItemsEnumeratorState", x => x.PlayoutAnchorPlayoutId);
                    table.ForeignKey(
                        name: "FK_ScheduleItemsEnumeratorState_PlayoutAnchor_PlayoutAnchorPlay~",
                        column: x => x.PlayoutAnchorPlayoutId,
                        principalTable: "PlayoutAnchor",
                        principalColumn: "PlayoutId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CollectionEnumeratorState",
                columns: table => new
                {
                    PlayoutProgramScheduleAnchorId = table.Column<int>(type: "int", nullable: false),
                    Seed = table.Column<int>(type: "int", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionEnumeratorState", x => x.PlayoutProgramScheduleAnchorId);
                    table.ForeignKey(
                        name: "FK_CollectionEnumeratorState_PlayoutProgramScheduleAnchor_Playo~",
                        column: x => x.PlayoutProgramScheduleAnchorId,
                        principalTable: "PlayoutProgramScheduleAnchor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Artwork",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Path = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourcePath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlurHash43 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlurHash54 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlurHash64 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtworkKind = table.Column<int>(type: "int", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ArtistMetadataId = table.Column<int>(type: "int", nullable: true),
                    ChannelId = table.Column<int>(type: "int", nullable: true),
                    EpisodeMetadataId = table.Column<int>(type: "int", nullable: true),
                    MovieMetadataId = table.Column<int>(type: "int", nullable: true),
                    MusicVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    OtherVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    SeasonMetadataId = table.Column<int>(type: "int", nullable: true),
                    ShowMetadataId = table.Column<int>(type: "int", nullable: true),
                    SongMetadataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artwork", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Artwork_ArtistMetadata_ArtistMetadataId",
                        column: x => x.ArtistMetadataId,
                        principalTable: "ArtistMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Artwork_Channel_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Artwork_EpisodeMetadata_EpisodeMetadataId",
                        column: x => x.EpisodeMetadataId,
                        principalTable: "EpisodeMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Artwork_MovieMetadata_MovieMetadataId",
                        column: x => x.MovieMetadataId,
                        principalTable: "MovieMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Artwork_MusicVideoMetadata_MusicVideoMetadataId",
                        column: x => x.MusicVideoMetadataId,
                        principalTable: "MusicVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Artwork_OtherVideoMetadata_OtherVideoMetadataId",
                        column: x => x.OtherVideoMetadataId,
                        principalTable: "OtherVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Artwork_SeasonMetadata_SeasonMetadataId",
                        column: x => x.SeasonMetadataId,
                        principalTable: "SeasonMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Artwork_ShowMetadata_ShowMetadataId",
                        column: x => x.ShowMetadataId,
                        principalTable: "ShowMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Artwork_SongMetadata_SongMetadataId",
                        column: x => x.SongMetadataId,
                        principalTable: "SongMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Director",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EpisodeMetadataId = table.Column<int>(type: "int", nullable: true),
                    MovieMetadataId = table.Column<int>(type: "int", nullable: true),
                    MusicVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    OtherVideoMetadataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Director", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Director_EpisodeMetadata_EpisodeMetadataId",
                        column: x => x.EpisodeMetadataId,
                        principalTable: "EpisodeMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Director_MovieMetadata_MovieMetadataId",
                        column: x => x.MovieMetadataId,
                        principalTable: "MovieMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Director_MusicVideoMetadata_MusicVideoMetadataId",
                        column: x => x.MusicVideoMetadataId,
                        principalTable: "MusicVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Director_OtherVideoMetadata_OtherVideoMetadataId",
                        column: x => x.OtherVideoMetadataId,
                        principalTable: "OtherVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Genre",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistMetadataId = table.Column<int>(type: "int", nullable: true),
                    EpisodeMetadataId = table.Column<int>(type: "int", nullable: true),
                    MovieMetadataId = table.Column<int>(type: "int", nullable: true),
                    MusicVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    OtherVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    SeasonMetadataId = table.Column<int>(type: "int", nullable: true),
                    ShowMetadataId = table.Column<int>(type: "int", nullable: true),
                    SongMetadataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genre", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Genre_ArtistMetadata_ArtistMetadataId",
                        column: x => x.ArtistMetadataId,
                        principalTable: "ArtistMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Genre_EpisodeMetadata_EpisodeMetadataId",
                        column: x => x.EpisodeMetadataId,
                        principalTable: "EpisodeMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Genre_MovieMetadata_MovieMetadataId",
                        column: x => x.MovieMetadataId,
                        principalTable: "MovieMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Genre_MusicVideoMetadata_MusicVideoMetadataId",
                        column: x => x.MusicVideoMetadataId,
                        principalTable: "MusicVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Genre_OtherVideoMetadata_OtherVideoMetadataId",
                        column: x => x.OtherVideoMetadataId,
                        principalTable: "OtherVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Genre_SeasonMetadata_SeasonMetadataId",
                        column: x => x.SeasonMetadataId,
                        principalTable: "SeasonMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Genre_ShowMetadata_ShowMetadataId",
                        column: x => x.ShowMetadataId,
                        principalTable: "ShowMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Genre_SongMetadata_SongMetadataId",
                        column: x => x.SongMetadataId,
                        principalTable: "SongMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MetadataGuid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistMetadataId = table.Column<int>(type: "int", nullable: true),
                    EpisodeMetadataId = table.Column<int>(type: "int", nullable: true),
                    MovieMetadataId = table.Column<int>(type: "int", nullable: true),
                    MusicVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    OtherVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    SeasonMetadataId = table.Column<int>(type: "int", nullable: true),
                    ShowMetadataId = table.Column<int>(type: "int", nullable: true),
                    SongMetadataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataGuid", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_ArtistMetadata_ArtistMetadataId",
                        column: x => x.ArtistMetadataId,
                        principalTable: "ArtistMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_EpisodeMetadata_EpisodeMetadataId",
                        column: x => x.EpisodeMetadataId,
                        principalTable: "EpisodeMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_MovieMetadata_MovieMetadataId",
                        column: x => x.MovieMetadataId,
                        principalTable: "MovieMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_MusicVideoMetadata_MusicVideoMetadataId",
                        column: x => x.MusicVideoMetadataId,
                        principalTable: "MusicVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_OtherVideoMetadata_OtherVideoMetadataId",
                        column: x => x.OtherVideoMetadataId,
                        principalTable: "OtherVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_SeasonMetadata_SeasonMetadataId",
                        column: x => x.SeasonMetadataId,
                        principalTable: "SeasonMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_ShowMetadata_ShowMetadataId",
                        column: x => x.ShowMetadataId,
                        principalTable: "ShowMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_SongMetadata_SongMetadataId",
                        column: x => x.SongMetadataId,
                        principalTable: "SongMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Studio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistMetadataId = table.Column<int>(type: "int", nullable: true),
                    EpisodeMetadataId = table.Column<int>(type: "int", nullable: true),
                    MovieMetadataId = table.Column<int>(type: "int", nullable: true),
                    MusicVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    OtherVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    SeasonMetadataId = table.Column<int>(type: "int", nullable: true),
                    ShowMetadataId = table.Column<int>(type: "int", nullable: true),
                    SongMetadataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Studio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Studio_ArtistMetadata_ArtistMetadataId",
                        column: x => x.ArtistMetadataId,
                        principalTable: "ArtistMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Studio_EpisodeMetadata_EpisodeMetadataId",
                        column: x => x.EpisodeMetadataId,
                        principalTable: "EpisodeMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Studio_MovieMetadata_MovieMetadataId",
                        column: x => x.MovieMetadataId,
                        principalTable: "MovieMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Studio_MusicVideoMetadata_MusicVideoMetadataId",
                        column: x => x.MusicVideoMetadataId,
                        principalTable: "MusicVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Studio_OtherVideoMetadata_OtherVideoMetadataId",
                        column: x => x.OtherVideoMetadataId,
                        principalTable: "OtherVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Studio_SeasonMetadata_SeasonMetadataId",
                        column: x => x.SeasonMetadataId,
                        principalTable: "SeasonMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Studio_ShowMetadata_ShowMetadataId",
                        column: x => x.ShowMetadataId,
                        principalTable: "ShowMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Studio_SongMetadata_SongMetadataId",
                        column: x => x.SongMetadataId,
                        principalTable: "SongMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Subtitle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubtitleKind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StreamIndex = table.Column<int>(type: "int", nullable: false),
                    Codec = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Default = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Forced = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SDH = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Language = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsExtracted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Path = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ArtistMetadataId = table.Column<int>(type: "int", nullable: true),
                    EpisodeMetadataId = table.Column<int>(type: "int", nullable: true),
                    MovieMetadataId = table.Column<int>(type: "int", nullable: true),
                    MusicVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    OtherVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    SeasonMetadataId = table.Column<int>(type: "int", nullable: true),
                    ShowMetadataId = table.Column<int>(type: "int", nullable: true),
                    SongMetadataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subtitle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subtitle_ArtistMetadata_ArtistMetadataId",
                        column: x => x.ArtistMetadataId,
                        principalTable: "ArtistMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtitle_EpisodeMetadata_EpisodeMetadataId",
                        column: x => x.EpisodeMetadataId,
                        principalTable: "EpisodeMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtitle_MovieMetadata_MovieMetadataId",
                        column: x => x.MovieMetadataId,
                        principalTable: "MovieMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtitle_MusicVideoMetadata_MusicVideoMetadataId",
                        column: x => x.MusicVideoMetadataId,
                        principalTable: "MusicVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtitle_OtherVideoMetadata_OtherVideoMetadataId",
                        column: x => x.OtherVideoMetadataId,
                        principalTable: "OtherVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtitle_SeasonMetadata_SeasonMetadataId",
                        column: x => x.SeasonMetadataId,
                        principalTable: "SeasonMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtitle_ShowMetadata_ShowMetadataId",
                        column: x => x.ShowMetadataId,
                        principalTable: "ShowMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtitle_SongMetadata_SongMetadataId",
                        column: x => x.SongMetadataId,
                        principalTable: "SongMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Tag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalCollectionId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistMetadataId = table.Column<int>(type: "int", nullable: true),
                    EpisodeMetadataId = table.Column<int>(type: "int", nullable: true),
                    MovieMetadataId = table.Column<int>(type: "int", nullable: true),
                    MusicVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    OtherVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    SeasonMetadataId = table.Column<int>(type: "int", nullable: true),
                    ShowMetadataId = table.Column<int>(type: "int", nullable: true),
                    SongMetadataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tag_ArtistMetadata_ArtistMetadataId",
                        column: x => x.ArtistMetadataId,
                        principalTable: "ArtistMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tag_EpisodeMetadata_EpisodeMetadataId",
                        column: x => x.EpisodeMetadataId,
                        principalTable: "EpisodeMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tag_MovieMetadata_MovieMetadataId",
                        column: x => x.MovieMetadataId,
                        principalTable: "MovieMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tag_MusicVideoMetadata_MusicVideoMetadataId",
                        column: x => x.MusicVideoMetadataId,
                        principalTable: "MusicVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tag_OtherVideoMetadata_OtherVideoMetadataId",
                        column: x => x.OtherVideoMetadataId,
                        principalTable: "OtherVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tag_SeasonMetadata_SeasonMetadataId",
                        column: x => x.SeasonMetadataId,
                        principalTable: "SeasonMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tag_ShowMetadata_ShowMetadataId",
                        column: x => x.ShowMetadataId,
                        principalTable: "ShowMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tag_SongMetadata_SongMetadataId",
                        column: x => x.SongMetadataId,
                        principalTable: "SongMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Writer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EpisodeMetadataId = table.Column<int>(type: "int", nullable: true),
                    MovieMetadataId = table.Column<int>(type: "int", nullable: true),
                    OtherVideoMetadataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Writer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Writer_EpisodeMetadata_EpisodeMetadataId",
                        column: x => x.EpisodeMetadataId,
                        principalTable: "EpisodeMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Writer_MovieMetadata_MovieMetadataId",
                        column: x => x.MovieMetadataId,
                        principalTable: "MovieMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Writer_OtherVideoMetadata_OtherVideoMetadataId",
                        column: x => x.OtherVideoMetadataId,
                        principalTable: "OtherVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MediaChapter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MediaVersionId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<long>(type: "bigint", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaChapter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaChapter_MediaVersion_MediaVersionId",
                        column: x => x.MediaVersionId,
                        principalTable: "MediaVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MediaFile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Path = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MediaVersionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaFile_MediaVersion_MediaVersionId",
                        column: x => x.MediaVersionId,
                        principalTable: "MediaVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MediaStream",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<int>(type: "int", nullable: false),
                    Codec = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Profile = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MediaStreamKind = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Channels = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Default = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Forced = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AttachedPic = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PixelFormat = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorRange = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorSpace = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorTransfer = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorPrimaries = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BitsPerRawSample = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MimeType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MediaVersionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaStream", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaStream_MediaVersion_MediaVersionId",
                        column: x => x.MediaVersionId,
                        principalTable: "MediaVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Actor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Order = table.Column<int>(type: "int", nullable: true),
                    ArtworkId = table.Column<int>(type: "int", nullable: true),
                    ArtistMetadataId = table.Column<int>(type: "int", nullable: true),
                    EpisodeMetadataId = table.Column<int>(type: "int", nullable: true),
                    MovieMetadataId = table.Column<int>(type: "int", nullable: true),
                    MusicVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    OtherVideoMetadataId = table.Column<int>(type: "int", nullable: true),
                    SeasonMetadataId = table.Column<int>(type: "int", nullable: true),
                    ShowMetadataId = table.Column<int>(type: "int", nullable: true),
                    SongMetadataId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actor_ArtistMetadata_ArtistMetadataId",
                        column: x => x.ArtistMetadataId,
                        principalTable: "ArtistMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Actor_Artwork_ArtworkId",
                        column: x => x.ArtworkId,
                        principalTable: "Artwork",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Actor_EpisodeMetadata_EpisodeMetadataId",
                        column: x => x.EpisodeMetadataId,
                        principalTable: "EpisodeMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Actor_MovieMetadata_MovieMetadataId",
                        column: x => x.MovieMetadataId,
                        principalTable: "MovieMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Actor_MusicVideoMetadata_MusicVideoMetadataId",
                        column: x => x.MusicVideoMetadataId,
                        principalTable: "MusicVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Actor_OtherVideoMetadata_OtherVideoMetadataId",
                        column: x => x.OtherVideoMetadataId,
                        principalTable: "OtherVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Actor_SeasonMetadata_SeasonMetadataId",
                        column: x => x.SeasonMetadataId,
                        principalTable: "SeasonMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Actor_ShowMetadata_ShowMetadataId",
                        column: x => x.ShowMetadataId,
                        principalTable: "ShowMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Actor_SongMetadata_SongMetadataId",
                        column: x => x.SongMetadataId,
                        principalTable: "SongMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlexMediaFile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    PlexId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexMediaFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlexMediaFile_MediaFile_Id",
                        column: x => x.Id,
                        principalTable: "MediaFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_ArtistMetadataId",
                table: "Actor",
                column: "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_ArtworkId",
                table: "Actor",
                column: "ArtworkId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actor_EpisodeMetadataId",
                table: "Actor",
                column: "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_MovieMetadataId",
                table: "Actor",
                column: "MovieMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_MusicVideoMetadataId",
                table: "Actor",
                column: "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_OtherVideoMetadataId",
                table: "Actor",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_SeasonMetadataId",
                table: "Actor",
                column: "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_ShowMetadataId",
                table: "Actor",
                column: "ShowMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_SongMetadataId",
                table: "Actor",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistMetadata_ArtistId",
                table: "ArtistMetadata",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_ArtistMetadataId",
                table: "Artwork",
                column: "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_ChannelId",
                table: "Artwork",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_EpisodeMetadataId",
                table: "Artwork",
                column: "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_MovieMetadataId",
                table: "Artwork",
                column: "MovieMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_MusicVideoMetadataId",
                table: "Artwork",
                column: "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_OtherVideoMetadataId",
                table: "Artwork",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_SeasonMetadataId",
                table: "Artwork",
                column: "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_ShowMetadataId",
                table: "Artwork",
                column: "ShowMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_SongMetadataId",
                table: "Artwork",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Channel_FallbackFillerId",
                table: "Channel",
                column: "FallbackFillerId");

            migrationBuilder.CreateIndex(
                name: "IX_Channel_FFmpegProfileId",
                table: "Channel",
                column: "FFmpegProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Channel_Number",
                table: "Channel",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channel_WatermarkId",
                table: "Channel",
                column: "WatermarkId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionItem_MediaItemId",
                table: "CollectionItem",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigElement_Key",
                table: "ConfigElement",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Director_EpisodeMetadataId",
                table: "Director",
                column: "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Director_MovieMetadataId",
                table: "Director",
                column: "MovieMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Director_MusicVideoMetadataId",
                table: "Director",
                column: "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Director_OtherVideoMetadataId",
                table: "Director",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_EmbyConnection_EmbyMediaSourceId",
                table: "EmbyConnection",
                column: "EmbyMediaSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_EmbyPathInfo_EmbyLibraryId",
                table: "EmbyPathInfo",
                column: "EmbyLibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmbyPathReplacement_EmbyMediaSourceId",
                table: "EmbyPathReplacement",
                column: "EmbyMediaSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Episode_SeasonId",
                table: "Episode",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeMetadata_EpisodeId",
                table: "EpisodeMetadata",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_FFmpegProfile_ResolutionId",
                table: "FFmpegProfile",
                column: "ResolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_FillerPreset_CollectionId",
                table: "FillerPreset",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FillerPreset_MediaItemId",
                table: "FillerPreset",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FillerPreset_MultiCollectionId",
                table: "FillerPreset",
                column: "MultiCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FillerPreset_SmartCollectionId",
                table: "FillerPreset",
                column: "SmartCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_ArtistMetadataId",
                table: "Genre",
                column: "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_EpisodeMetadataId",
                table: "Genre",
                column: "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_MovieMetadataId",
                table: "Genre",
                column: "MovieMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_MusicVideoMetadataId",
                table: "Genre",
                column: "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_OtherVideoMetadataId",
                table: "Genre",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_SeasonMetadataId",
                table: "Genre",
                column: "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_ShowMetadataId",
                table: "Genre",
                column: "ShowMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_SongMetadataId",
                table: "Genre",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_JellyfinConnection_JellyfinMediaSourceId",
                table: "JellyfinConnection",
                column: "JellyfinMediaSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_JellyfinPathInfo_JellyfinLibraryId",
                table: "JellyfinPathInfo",
                column: "JellyfinLibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_JellyfinPathReplacement_JellyfinMediaSourceId",
                table: "JellyfinPathReplacement",
                column: "JellyfinMediaSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Library_MediaSourceId",
                table: "Library",
                column: "MediaSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryFolder_LibraryPathId",
                table: "LibraryFolder",
                column: "LibraryPathId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryPath_LibraryId",
                table: "LibraryPath",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaChapter_MediaVersionId",
                table: "MediaChapter",
                column: "MediaVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFile_MediaVersionId",
                table: "MediaFile",
                column: "MediaVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFile_Path",
                table: "MediaFile",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaItem_LibraryPathId",
                table: "MediaItem",
                column: "LibraryPathId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaStream_MediaVersionId",
                table: "MediaStream",
                column: "MediaVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaVersion_EpisodeId",
                table: "MediaVersion",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaVersion_MovieId",
                table: "MediaVersion",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaVersion_MusicVideoId",
                table: "MediaVersion",
                column: "MusicVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaVersion_OtherVideoId",
                table: "MediaVersion",
                column: "OtherVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaVersion_SongId",
                table: "MediaVersion",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_ArtistMetadataId",
                table: "MetadataGuid",
                column: "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_EpisodeMetadataId",
                table: "MetadataGuid",
                column: "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_MovieMetadataId",
                table: "MetadataGuid",
                column: "MovieMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_MusicVideoMetadataId",
                table: "MetadataGuid",
                column: "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_OtherVideoMetadataId",
                table: "MetadataGuid",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_SeasonMetadataId",
                table: "MetadataGuid",
                column: "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_ShowMetadataId",
                table: "MetadataGuid",
                column: "ShowMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_SongMetadataId",
                table: "MetadataGuid",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Mood_ArtistMetadataId",
                table: "Mood",
                column: "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieMetadata_MovieId",
                table: "MovieMetadata",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiCollectionItem_CollectionId",
                table: "MultiCollectionItem",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiCollectionSmartItem_SmartCollectionId",
                table: "MultiCollectionSmartItem",
                column: "SmartCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicVideo_ArtistId",
                table: "MusicVideo",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicVideoArtist_MusicVideoMetadataId",
                table: "MusicVideoArtist",
                column: "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicVideoMetadata_MusicVideoId",
                table: "MusicVideoMetadata",
                column: "MusicVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_OtherVideoMetadata_OtherVideoId",
                table: "OtherVideoMetadata",
                column: "OtherVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_Playout_ChannelId",
                table: "Playout",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Playout_ProgramScheduleId",
                table: "Playout",
                column: "ProgramScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutItem_MediaItemId",
                table: "PlayoutItem",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutItem_PlayoutId",
                table: "PlayoutItem",
                column: "PlayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutItem_WatermarkId",
                table: "PlayoutItem",
                column: "WatermarkId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutProgramScheduleAnchor_CollectionId",
                table: "PlayoutProgramScheduleAnchor",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutProgramScheduleAnchor_MediaItemId",
                table: "PlayoutProgramScheduleAnchor",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutProgramScheduleAnchor_MultiCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                column: "MultiCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutProgramScheduleAnchor_PlayoutId",
                table: "PlayoutProgramScheduleAnchor",
                column: "PlayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutProgramScheduleAnchor_SmartCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                column: "SmartCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlexConnection_PlexMediaSourceId",
                table: "PlexConnection",
                column: "PlexMediaSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlexPathReplacement_PlexMediaSourceId",
                table: "PlexPathReplacement",
                column: "PlexMediaSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramSchedule_Name",
                table: "ProgramSchedule",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleAlternate_PlayoutId",
                table: "ProgramScheduleAlternate",
                column: "PlayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleAlternate_ProgramScheduleId",
                table: "ProgramScheduleAlternate",
                column: "ProgramScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_CollectionId",
                table: "ProgramScheduleItem",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_FallbackFillerId",
                table: "ProgramScheduleItem",
                column: "FallbackFillerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_MediaItemId",
                table: "ProgramScheduleItem",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_MidRollFillerId",
                table: "ProgramScheduleItem",
                column: "MidRollFillerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_MultiCollectionId",
                table: "ProgramScheduleItem",
                column: "MultiCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_PostRollFillerId",
                table: "ProgramScheduleItem",
                column: "PostRollFillerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_PreRollFillerId",
                table: "ProgramScheduleItem",
                column: "PreRollFillerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_ProgramScheduleId",
                table: "ProgramScheduleItem",
                column: "ProgramScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_SmartCollectionId",
                table: "ProgramScheduleItem",
                column: "SmartCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_TailFillerId",
                table: "ProgramScheduleItem",
                column: "TailFillerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_WatermarkId",
                table: "ProgramScheduleItem",
                column: "WatermarkId");

            migrationBuilder.CreateIndex(
                name: "IX_Season_ShowId",
                table: "Season",
                column: "ShowId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonMetadata_SeasonId",
                table: "SeasonMetadata",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowMetadata_ShowId",
                table: "ShowMetadata",
                column: "ShowId");

            migrationBuilder.CreateIndex(
                name: "IX_SongMetadata_SongId",
                table: "SongMetadata",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_Studio_ArtistMetadataId",
                table: "Studio",
                column: "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Studio_EpisodeMetadataId",
                table: "Studio",
                column: "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Studio_MovieMetadataId",
                table: "Studio",
                column: "MovieMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Studio_MusicVideoMetadataId",
                table: "Studio",
                column: "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Studio_OtherVideoMetadataId",
                table: "Studio",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Studio_SeasonMetadataId",
                table: "Studio",
                column: "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Studio_ShowMetadataId",
                table: "Studio",
                column: "ShowMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Studio_SongMetadataId",
                table: "Studio",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Style_ArtistMetadataId",
                table: "Style",
                column: "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_ArtistMetadataId",
                table: "Subtitle",
                column: "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_EpisodeMetadataId",
                table: "Subtitle",
                column: "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_MovieMetadataId",
                table: "Subtitle",
                column: "MovieMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_MusicVideoMetadataId",
                table: "Subtitle",
                column: "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_OtherVideoMetadataId",
                table: "Subtitle",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_SeasonMetadataId",
                table: "Subtitle",
                column: "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_ShowMetadataId",
                table: "Subtitle",
                column: "ShowMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_SongMetadataId",
                table: "Subtitle",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_ArtistMetadataId",
                table: "Tag",
                column: "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_EpisodeMetadataId",
                table: "Tag",
                column: "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_MovieMetadataId",
                table: "Tag",
                column: "MovieMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_MusicVideoMetadataId",
                table: "Tag",
                column: "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_OtherVideoMetadataId",
                table: "Tag",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_SeasonMetadataId",
                table: "Tag",
                column: "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_ShowMetadataId",
                table: "Tag",
                column: "ShowMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_SongMetadataId",
                table: "Tag",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_TraktListItem_MediaItemId",
                table: "TraktListItem",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TraktListItem_TraktListId",
                table: "TraktListItem",
                column: "TraktListId");

            migrationBuilder.CreateIndex(
                name: "IX_TraktListItemGuid_TraktListItemId",
                table: "TraktListItemGuid",
                column: "TraktListItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Writer_EpisodeMetadataId",
                table: "Writer",
                column: "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Writer_MovieMetadataId",
                table: "Writer",
                column: "MovieMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Writer_OtherVideoMetadataId",
                table: "Writer",
                column: "OtherVideoMetadataId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Actor");

            migrationBuilder.DropTable(
                name: "CollectionEnumeratorState");

            migrationBuilder.DropTable(
                name: "CollectionItem");

            migrationBuilder.DropTable(
                name: "ConfigElement");

            migrationBuilder.DropTable(
                name: "Director");

            migrationBuilder.DropTable(
                name: "EmbyCollection");

            migrationBuilder.DropTable(
                name: "EmbyConnection");

            migrationBuilder.DropTable(
                name: "EmbyEpisode");

            migrationBuilder.DropTable(
                name: "EmbyMovie");

            migrationBuilder.DropTable(
                name: "EmbyPathInfo");

            migrationBuilder.DropTable(
                name: "EmbyPathReplacement");

            migrationBuilder.DropTable(
                name: "EmbySeason");

            migrationBuilder.DropTable(
                name: "EmbyShow");

            migrationBuilder.DropTable(
                name: "Genre");

            migrationBuilder.DropTable(
                name: "JellyfinCollection");

            migrationBuilder.DropTable(
                name: "JellyfinConnection");

            migrationBuilder.DropTable(
                name: "JellyfinEpisode");

            migrationBuilder.DropTable(
                name: "JellyfinMovie");

            migrationBuilder.DropTable(
                name: "JellyfinPathInfo");

            migrationBuilder.DropTable(
                name: "JellyfinPathReplacement");

            migrationBuilder.DropTable(
                name: "JellyfinSeason");

            migrationBuilder.DropTable(
                name: "JellyfinShow");

            migrationBuilder.DropTable(
                name: "LanguageCode");

            migrationBuilder.DropTable(
                name: "LibraryFolder");

            migrationBuilder.DropTable(
                name: "LocalLibrary");

            migrationBuilder.DropTable(
                name: "LocalMediaSource");

            migrationBuilder.DropTable(
                name: "MediaChapter");

            migrationBuilder.DropTable(
                name: "MediaStream");

            migrationBuilder.DropTable(
                name: "MetadataGuid");

            migrationBuilder.DropTable(
                name: "Mood");

            migrationBuilder.DropTable(
                name: "MultiCollectionItem");

            migrationBuilder.DropTable(
                name: "MultiCollectionSmartItem");

            migrationBuilder.DropTable(
                name: "MusicVideoArtist");

            migrationBuilder.DropTable(
                name: "PlayoutItem");

            migrationBuilder.DropTable(
                name: "PlexConnection");

            migrationBuilder.DropTable(
                name: "PlexEpisode");

            migrationBuilder.DropTable(
                name: "PlexLibrary");

            migrationBuilder.DropTable(
                name: "PlexMediaFile");

            migrationBuilder.DropTable(
                name: "PlexMovie");

            migrationBuilder.DropTable(
                name: "PlexPathReplacement");

            migrationBuilder.DropTable(
                name: "PlexSeason");

            migrationBuilder.DropTable(
                name: "PlexShow");

            migrationBuilder.DropTable(
                name: "ProgramScheduleAlternate");

            migrationBuilder.DropTable(
                name: "ProgramScheduleDurationItem");

            migrationBuilder.DropTable(
                name: "ProgramScheduleFloodItem");

            migrationBuilder.DropTable(
                name: "ProgramScheduleMultipleItem");

            migrationBuilder.DropTable(
                name: "ProgramScheduleOneItem");

            migrationBuilder.DropTable(
                name: "ScheduleItemsEnumeratorState");

            migrationBuilder.DropTable(
                name: "Studio");

            migrationBuilder.DropTable(
                name: "Style");

            migrationBuilder.DropTable(
                name: "Subtitle");

            migrationBuilder.DropTable(
                name: "Tag");

            migrationBuilder.DropTable(
                name: "TraktListItemGuid");

            migrationBuilder.DropTable(
                name: "Writer");

            migrationBuilder.DropTable(
                name: "Artwork");

            migrationBuilder.DropTable(
                name: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropTable(
                name: "EmbyLibrary");

            migrationBuilder.DropTable(
                name: "EmbyMediaSource");

            migrationBuilder.DropTable(
                name: "JellyfinLibrary");

            migrationBuilder.DropTable(
                name: "JellyfinMediaSource");

            migrationBuilder.DropTable(
                name: "MediaFile");

            migrationBuilder.DropTable(
                name: "PlexMediaSource");

            migrationBuilder.DropTable(
                name: "ProgramScheduleItem");

            migrationBuilder.DropTable(
                name: "PlayoutAnchor");

            migrationBuilder.DropTable(
                name: "TraktListItem");

            migrationBuilder.DropTable(
                name: "ArtistMetadata");

            migrationBuilder.DropTable(
                name: "EpisodeMetadata");

            migrationBuilder.DropTable(
                name: "MovieMetadata");

            migrationBuilder.DropTable(
                name: "MusicVideoMetadata");

            migrationBuilder.DropTable(
                name: "OtherVideoMetadata");

            migrationBuilder.DropTable(
                name: "SeasonMetadata");

            migrationBuilder.DropTable(
                name: "ShowMetadata");

            migrationBuilder.DropTable(
                name: "SongMetadata");

            migrationBuilder.DropTable(
                name: "MediaVersion");

            migrationBuilder.DropTable(
                name: "Playout");

            migrationBuilder.DropTable(
                name: "TraktList");

            migrationBuilder.DropTable(
                name: "Episode");

            migrationBuilder.DropTable(
                name: "Movie");

            migrationBuilder.DropTable(
                name: "MusicVideo");

            migrationBuilder.DropTable(
                name: "OtherVideo");

            migrationBuilder.DropTable(
                name: "Song");

            migrationBuilder.DropTable(
                name: "Channel");

            migrationBuilder.DropTable(
                name: "ProgramSchedule");

            migrationBuilder.DropTable(
                name: "Season");

            migrationBuilder.DropTable(
                name: "Artist");

            migrationBuilder.DropTable(
                name: "ChannelWatermark");

            migrationBuilder.DropTable(
                name: "FFmpegProfile");

            migrationBuilder.DropTable(
                name: "FillerPreset");

            migrationBuilder.DropTable(
                name: "Show");

            migrationBuilder.DropTable(
                name: "Resolution");

            migrationBuilder.DropTable(
                name: "Collection");

            migrationBuilder.DropTable(
                name: "MultiCollection");

            migrationBuilder.DropTable(
                name: "SmartCollection");

            migrationBuilder.DropTable(
                name: "MediaItem");

            migrationBuilder.DropTable(
                name: "LibraryPath");

            migrationBuilder.DropTable(
                name: "Library");

            migrationBuilder.DropTable(
                name: "MediaSource");
        }
    }
}
