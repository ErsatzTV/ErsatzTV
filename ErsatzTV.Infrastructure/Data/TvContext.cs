using System.Data;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Data;

public class TvContext : DbContext
{
    private readonly ILoggerFactory _loggerFactory;

    public TvContext(DbContextOptions<TvContext> options, ILoggerFactory loggerFactory)
        : base(options) =>
        _loggerFactory = loggerFactory;

    public static string LastInsertedRowId { get; set; } = "last_insert_rowid()";
    public static string CaseInsensitiveCollation { get; set; } = "NOCASE";

    public IDbConnection Connection => Database.GetDbConnection();

    public DbSet<ConfigElement> ConfigElements { get; set; }
    public DbSet<Channel> Channels { get; set; }
    public DbSet<ChannelWatermark> ChannelWatermarks { get; set; }
    public DbSet<MediaSource> MediaSources { get; set; }
    public DbSet<LocalMediaSource> LocalMediaSources { get; set; }
    public DbSet<PlexMediaSource> PlexMediaSources { get; set; }
    public DbSet<JellyfinMediaSource> JellyfinMediaSources { get; set; }
    public DbSet<EmbyMediaSource> EmbyMediaSources { get; set; }
    public DbSet<Library> Libraries { get; set; }
    public DbSet<LocalLibrary> LocalLibraries { get; set; }
    public DbSet<LibraryPath> LibraryPaths { get; set; }
    public DbSet<LibraryFolder> LibraryFolders { get; set; }
    public DbSet<PlexLibrary> PlexLibraries { get; set; }
    public DbSet<JellyfinLibrary> JellyfinLibraries { get; set; }
    public DbSet<EmbyLibrary> EmbyLibraries { get; set; }
    public DbSet<PlexPathReplacement> PlexPathReplacements { get; set; }
    public DbSet<JellyfinPathReplacement> JellyfinPathReplacements { get; set; }
    public DbSet<EmbyPathReplacement> EmbyPathReplacements { get; set; }
    public DbSet<MediaItem> MediaItems { get; set; }
    public DbSet<MediaVersion> MediaVersions { get; set; }
    public DbSet<MediaFile> MediaFiles { get; set; }
    public DbSet<MediaStream> MediaStreams { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<MovieMetadata> MovieMetadata { get; set; }
    public DbSet<Artwork> Artwork { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<ArtistMetadata> ArtistMetadata { get; set; }
    public DbSet<MusicVideo> MusicVideos { get; set; }
    public DbSet<MusicVideoMetadata> MusicVideoMetadata { get; set; }
    public DbSet<OtherVideo> OtherVideos { get; set; }
    public DbSet<OtherVideoMetadata> OtherVideoMetadata { get; set; }
    public DbSet<Song> Songs { get; set; }
    public DbSet<SongMetadata> SongMetadata { get; set; }
    public DbSet<Show> Shows { get; set; }
    public DbSet<ShowMetadata> ShowMetadata { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<SeasonMetadata> SeasonMetadata { get; set; }
    public DbSet<Episode> Episodes { get; set; }
    public DbSet<EpisodeMetadata> EpisodeMetadata { get; set; }
    public DbSet<PlexMovie> PlexMovies { get; set; }
    public DbSet<PlexShow> PlexShows { get; set; }
    public DbSet<PlexSeason> PlexSeasons { get; set; }
    public DbSet<PlexEpisode> PlexEpisodes { get; set; }
    public DbSet<PlexCollection> PlexCollections { get; set; }
    public DbSet<JellyfinMovie> JellyfinMovies { get; set; }
    public DbSet<JellyfinShow> JellyfinShows { get; set; }
    public DbSet<JellyfinSeason> JellyfinSeasons { get; set; }
    public DbSet<JellyfinEpisode> JellyfinEpisodes { get; set; }
    public DbSet<JellyfinCollection> JellyfinCollections { get; set; }
    public DbSet<EmbyMovie> EmbyMovies { get; set; }
    public DbSet<EmbyShow> EmbyShows { get; set; }
    public DbSet<EmbySeason> EmbySeasons { get; set; }
    public DbSet<EmbyEpisode> EmbyEpisodes { get; set; }
    public DbSet<EmbyCollection> EmbyCollections { get; set; }
    public DbSet<Collection> Collections { get; set; }
    public DbSet<CollectionItem> CollectionItems { get; set; }
    public DbSet<MultiCollection> MultiCollections { get; set; }
    public DbSet<SmartCollection> SmartCollections { get; set; }
    public DbSet<ProgramSchedule> ProgramSchedules { get; set; }
    public DbSet<ProgramScheduleItem> ProgramScheduleItems { get; set; }
    public DbSet<Playout> Playouts { get; set; }
    public DbSet<ProgramScheduleAlternate> ProgramScheduleAlternates { get; set; }
    public DbSet<PlayoutItem> PlayoutItems { get; set; }
    public DbSet<PlayoutProgramScheduleAnchor> PlayoutProgramScheduleItemAnchors { get; set; }
    public DbSet<BlockGroup> BlockGroups { get; set; }
    public DbSet<Block> Blocks { get; set; }
    public DbSet<BlockItem> BlockItems { get; set; }
    public DbSet<TemplateGroup> TemplateGroups { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<TemplateItem> TemplateItems { get; set; }
    public DbSet<FFmpegProfile> FFmpegProfiles { get; set; }
    public DbSet<Resolution> Resolutions { get; set; }
    public DbSet<LanguageCode> LanguageCodes { get; set; }
    public DbSet<TraktList> TraktLists { get; set; }
    public DbSet<FillerPreset> FillerPresets { get; set; }
    public DbSet<Subtitle> Subtitles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseLoggerFactory(_loggerFactory);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TvContext).Assembly);
    }
}
