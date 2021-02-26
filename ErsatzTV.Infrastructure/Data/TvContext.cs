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
        public DbSet<Library> Libraries { get; set; }
        public DbSet<PlexLibrary> PlexLibraries { get; set; }
        public DbSet<MediaItem> MediaItems { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<MovieMetadata> MovieMetadata { get; set; }
        public DbSet<Show> Shows { get; set; }
        public DbSet<ShowMetadata> ShowMetadata { get; set; }
        public DbSet<Season> Seasons { get; set; }
        public DbSet<Episode> Episodes { get; set; }
        public DbSet<PlexMovie> PlexMovieMediaItems { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<ProgramSchedule> ProgramSchedules { get; set; }
        public DbSet<Playout> Playouts { get; set; }
        public DbSet<PlayoutItem> PlayoutItems { get; set; }
        public DbSet<PlayoutProgramScheduleAnchor> PlayoutProgramScheduleItemAnchors { get; set; }
        public DbSet<FFmpegProfile> FFmpegProfiles { get; set; }
        public DbSet<Resolution> Resolutions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseLoggerFactory(_loggerFactory);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(TvContext).Assembly);
        }
    }
}
