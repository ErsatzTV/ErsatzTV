using ErsatzTV.Core.AggregateModels;
using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Data
{
    public class TvContext : DbContext
    {
        private readonly ILoggerFactory _loggerFactory;

        public TvContext(DbContextOptions<TvContext> options, ILoggerFactory loggerFactory)
            : base(options) =>
            _loggerFactory = loggerFactory;

        public DbSet<ConfigElement> ConfigElements { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<MediaSource> MediaSources { get; set; }
        public DbSet<LocalMediaSource> LocalMediaSources { get; set; }
        public DbSet<PlexMediaSource> PlexMediaSources { get; set; }
        public DbSet<MediaItem> MediaItems { get; set; }
        public DbSet<MovieMediaItem> MovieMediaItems { get; set; }
        public DbSet<TelevisionEpisodeMediaItem> TelevisionEpisodeMediaItems { get; set; }
        public DbSet<MediaCollection> MediaCollections { get; set; }
        public DbSet<SimpleMediaCollection> SimpleMediaCollections { get; set; }
        public DbSet<TelevisionMediaCollection> TelevisionMediaCollections { get; set; }
        public DbSet<ProgramSchedule> ProgramSchedules { get; set; }
        public DbSet<Playout> Playouts { get; set; }
        public DbSet<PlayoutItem> PlayoutItems { get; set; }
        public DbSet<PlayoutProgramScheduleAnchor> PlayoutProgramScheduleItemAnchors { get; set; }
        public DbSet<FFmpegProfile> FFmpegProfiles { get; set; }
        public DbSet<Resolution> Resolutions { get; set; }
        public DbSet<TelevisionShow> TelevisionShows { get; set; }
        public DbSet<TelevisionShowMetadata> TelevisionShowMetadata { get; set; }
        public DbSet<TelevisionSeason> TelevisionSeasons { get; set; }

        // support raw sql queries
        public DbSet<MediaCollectionSummary> MediaCollectionSummaries { get; set; }
        public DbSet<GenericIntegerId> GenericIntegerIds { get; set; }
        public DbSet<MediaItemSummary> MediaItemSummaries { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseLoggerFactory(_loggerFactory);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Ignore<MediaCollectionSummary>();
            builder.Ignore<GenericIntegerId>();
            builder.Ignore<MediaItemSummary>();

            builder.ApplyConfigurationsFromAssembly(typeof(TvContext).Assembly);
        }
    }
}
