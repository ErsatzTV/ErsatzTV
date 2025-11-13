using Bugsnag;
using Dapper;
using Destructurama;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Data.Repositories;
using ErsatzTV.Infrastructure.Extensions;
using ErsatzTV.Infrastructure.Metadata;
using ErsatzTV.Infrastructure.Search;
using ErsatzTV.Infrastructure.Sqlite.Data;
using LanguageExt.UnsafeValueAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
[Explicit]
public class ScheduleIntegrationTests
{
    [SetUp]
    public void SetUp() => _cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;

    private CancellationToken _cancellationToken;

    public ScheduleIntegrationTests() =>
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .WriteTo.Console()
            .Destructure.UsingAttributes()
            .CreateLogger();

    [Test]
    public async Task TestExistingData()
    {
        const string DB_FILE_NAME = "/tmp/whatever.sqlite3";
        const int PLAYOUT_ID = 39;

        var start = new DateTimeOffset(2023, 1, 18, 11, 0, 0, TimeSpan.FromHours(-5));
        DateTimeOffset finish = start.AddDays(2);

        IServiceCollection services = new ServiceCollection()
            .AddLogging();

        var connectionString = $"Data Source={DB_FILE_NAME};foreign keys=true;";

        services.AddDbContext<TvContext>(
            options => options.UseSqlite(
                connectionString,
                o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    o.MigrationsAssembly("ErsatzTV.Infrastructure.Sqlite");
                }),
            ServiceLifetime.Scoped,
            ServiceLifetime.Singleton);

        services.AddDbContextFactory<TvContext>(options => options.UseSqlite(
            connectionString,
            o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                o.MigrationsAssembly("ErsatzTV.Infrastructure.Sqlite");
            }));

        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());

        services.AddSingleton((Func<IServiceProvider, ILoggerFactory>)(_ => new SerilogLoggerFactory()));

        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<ILanguageCodeService, LanguageCodeService>();
        services.AddScoped<IConfigElementRepository, ConfigElementRepository>();
        services.AddScoped<IFallbackMetadataProvider, FallbackMetadataProvider>();

        services.AddSingleton<ISearchIndex, LuceneSearchIndex>();
        services.AddSingleton<ILanguageCodeCache, LanguageCodeCache>();

        services.AddSingleton(_ => Substitute.For<IClient>());

        ServiceProvider provider = services.BuildServiceProvider();

        IDbContextFactory<TvContext> factory = provider.GetRequiredService<IDbContextFactory<TvContext>>();

        ILogger<ScheduleIntegrationTests> logger = provider.GetRequiredService<ILogger<ScheduleIntegrationTests>>();
        logger.LogInformation("Database is at {File}", DB_FILE_NAME);

        await using TvContext dbContext = await factory.CreateDbContextAsync(CancellationToken.None);
        await dbContext.Database.MigrateAsync(CancellationToken.None);
        await DbInitializer.Initialize(dbContext, CancellationToken.None);

        ISearchIndex searchIndex = provider.GetRequiredService<ISearchIndex>();
        await searchIndex.Initialize(
            new LocalFileSystem(
                provider.GetRequiredService<IClient>(),
                provider.GetRequiredService<ILogger<LocalFileSystem>>()),
            provider.GetRequiredService<IConfigElementRepository>(),
            _cancellationToken);

        await searchIndex.Rebuild(
            provider.GetRequiredService<ISearchRepository>(),
            provider.GetRequiredService<IFallbackMetadataProvider>(),
            provider.GetRequiredService<ILanguageCodeService>(),
            _cancellationToken);

        var builder = new PlayoutBuilder(
            new ConfigElementRepository(factory),
            new MediaCollectionRepository(Substitute.For<IClient>(), searchIndex, factory),
            new TelevisionRepository(factory, provider.GetRequiredService<ILogger<TelevisionRepository>>()),
            new ArtistRepository(factory),
            Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>(),
            Substitute.For<ILocalFileSystem>(),
            Substitute.For<IRerunHelper>(),
            provider.GetRequiredService<ILogger<PlayoutBuilder>>());

        {
            await using TvContext context = await factory.CreateDbContextAsync(_cancellationToken);

            Option<Playout> maybePlayout = await GetPlayout(context, PLAYOUT_ID, _cancellationToken);
            Playout playout = maybePlayout.ValueUnsafe();
            PlayoutReferenceData referenceData = await GetReferenceData(
                context,
                PLAYOUT_ID,
                PlayoutScheduleKind.Classic);

            await builder.Build(
                playout,
                referenceData,
                PlayoutBuildResult.Empty,
                PlayoutBuildMode.Reset,
                start,
                finish,
                _cancellationToken);

            // TODO: would need to apply changes from build result
            await context.SaveChangesAsync(_cancellationToken);
        }

        for (var i = 1; i <= 24 * 1; i++)
        {
            await using TvContext context = await factory.CreateDbContextAsync(_cancellationToken);

            Option<Playout> maybePlayout = await GetPlayout(context, PLAYOUT_ID, _cancellationToken);
            Playout playout = maybePlayout.ValueUnsafe();
            PlayoutReferenceData referenceData = await GetReferenceData(
                context,
                PLAYOUT_ID,
                PlayoutScheduleKind.Classic);

            await builder.Build(
                playout,
                referenceData,
                PlayoutBuildResult.Empty,
                PlayoutBuildMode.Continue,
                start.AddHours(i),
                finish.AddHours(i),
                _cancellationToken);

            // TODO: would need to apply changes from build result
            await context.SaveChangesAsync(_cancellationToken);
        }

        for (var i = 25; i <= 26; i++)
        {
            await using TvContext context = await factory.CreateDbContextAsync(_cancellationToken);

            Option<Playout> maybePlayout = await GetPlayout(context, PLAYOUT_ID, _cancellationToken);
            Playout playout = maybePlayout.ValueUnsafe();
            PlayoutReferenceData referenceData = await GetReferenceData(
                context,
                PLAYOUT_ID,
                PlayoutScheduleKind.Classic);

            await builder.Build(
                playout,
                referenceData,
                PlayoutBuildResult.Empty,
                PlayoutBuildMode.Continue,
                start.AddHours(i),
                finish.AddHours(i),
                _cancellationToken);

            // TODO: would need to apply changes from build result
            await context.SaveChangesAsync(_cancellationToken);
        }
    }

    [Test]
    public async Task TestMockData()
    {
        string dbFileName = Path.GetTempFileName() + ".sqlite3";

        IServiceCollection services = new ServiceCollection()
            .AddLogging();

        var connectionString = $"Data Source={dbFileName};foreign keys=true;";

        services.AddDbContext<TvContext>(
            options => options.UseSqlite(
                connectionString,
                o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    o.MigrationsAssembly("ErsatzTV.Infrastructure.Sqlite");
                }),
            ServiceLifetime.Scoped,
            ServiceLifetime.Singleton);

        services.AddDbContextFactory<TvContext>(options => options.UseSqlite(
            connectionString,
            o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                o.MigrationsAssembly("ErsatzTV.Infrastructure.Sqlite");
            }));

        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());

        services.AddSingleton((Func<IServiceProvider, ILoggerFactory>)(_ => new SerilogLoggerFactory()));

        ServiceProvider provider = services.BuildServiceProvider();

        IDbContextFactory<TvContext> factory = provider.GetRequiredService<IDbContextFactory<TvContext>>();

        ILogger<ScheduleIntegrationTests> logger = provider.GetRequiredService<ILogger<ScheduleIntegrationTests>>();
        logger.LogInformation("Database is at {File}", dbFileName);

        await using TvContext dbContext = await factory.CreateDbContextAsync(CancellationToken.None);
        await dbContext.Database.MigrateAsync(CancellationToken.None);
        await DbInitializer.Initialize(dbContext, CancellationToken.None);

        var path = new LibraryPath
        {
            Path = "Test LibraryPath"
        };

        var library = new LocalLibrary
        {
            MediaKind = LibraryMediaKind.Movies,
            Paths = new List<LibraryPath> { path },
            MediaSource = new LocalMediaSource()
        };

        await dbContext.Libraries.AddAsync(library, _cancellationToken);
        await dbContext.SaveChangesAsync(_cancellationToken);

        var movies = new List<Movie>();
        for (var i = 1; i < 25; i++)
        {
            var movie = new Movie
            {
                MediaVersions = new List<MediaVersion>
                {
                    new() { Duration = TimeSpan.FromMinutes(55) }
                },
                MovieMetadata = new List<MovieMetadata>
                {
                    new()
                    {
                        Title = $"Movie {i}",
                        ReleaseDate = new DateTime(2000, 1, 1).AddDays(i)
                    }
                },
                LibraryPath = path,
                LibraryPathId = path.Id
            };

            movies.Add(movie);
        }

        await dbContext.Movies.AddRangeAsync(movies, _cancellationToken);
        await dbContext.SaveChangesAsync(_cancellationToken);

        var collection = new Collection
        {
            Name = "Test Collection",
            MediaItems = movies.Cast<MediaItem>().ToList()
        };

        await dbContext.Collections.AddAsync(collection, _cancellationToken);
        await dbContext.SaveChangesAsync(_cancellationToken);

        var scheduleItems = new List<ProgramScheduleItem>
        {
            new ProgramScheduleItemDuration
            {
                Collection = collection,
                CollectionId = collection.Id,
                PlayoutDuration = TimeSpan.FromHours(1),
                TailMode = TailMode.None, // immediately continue
                PlaybackOrder = PlaybackOrder.Shuffle
            }
        };

        int playoutId = await AddTestData(dbContext, scheduleItems);

        var start = new DateTimeOffset(2022, 7, 26, 8, 0, 5, TimeSpan.FromHours(-5));
        DateTimeOffset finish = start.AddDays(2);

        var builder = new PlayoutBuilder(
            new ConfigElementRepository(factory),
            new MediaCollectionRepository(Substitute.For<IClient>(), Substitute.For<ISearchIndex>(), factory),
            new TelevisionRepository(factory, provider.GetRequiredService<ILogger<TelevisionRepository>>()),
            new ArtistRepository(factory),
            Substitute.For<IMultiEpisodeShuffleCollectionEnumeratorFactory>(),
            Substitute.For<ILocalFileSystem>(),
            Substitute.For<IRerunHelper>(),
            provider.GetRequiredService<ILogger<PlayoutBuilder>>());

        for (var i = 0; i <= 24 * 4; i++)
        {
            await using TvContext context = await factory.CreateDbContextAsync(_cancellationToken);

            Option<Playout> maybePlayout = await GetPlayout(context, playoutId, _cancellationToken);
            Playout playout = maybePlayout.ValueUnsafe();
            PlayoutReferenceData referenceData = await GetReferenceData(
                context,
                playoutId,
                PlayoutScheduleKind.Classic);

            await builder.Build(
                playout,
                referenceData,
                PlayoutBuildResult.Empty,
                PlayoutBuildMode.Continue,
                start.AddHours(i),
                finish.AddHours(i),
                _cancellationToken);

            // TODO: would need to apply changes from build result
            await context.SaveChangesAsync(_cancellationToken);
        }
    }

    private static async Task<int> AddTestData(TvContext dbContext, List<ProgramScheduleItem> scheduleItems)
    {
        var ffmpegProfile = new FFmpegProfile
        {
            Name = "Test FFmpeg Profile"
        };

        await dbContext.FFmpegProfiles.AddAsync(ffmpegProfile);
        await dbContext.SaveChangesAsync();

        var channel = new Channel(Guid.Parse("00000000-0000-0000-0000-000000000001"))
        {
            Name = "Test Channel",
            FFmpegProfile = ffmpegProfile,
            FFmpegProfileId = ffmpegProfile.Id
        };

        await dbContext.Channels.AddAsync(channel);
        await dbContext.SaveChangesAsync();

        var schedule = new ProgramSchedule
        {
            Name = "Test Schedule",
            Items = scheduleItems
        };

        await dbContext.ProgramSchedules.AddAsync(schedule);
        await dbContext.SaveChangesAsync();

        var playout = new Playout
        {
            Channel = channel,
            ChannelId = channel.Id,
            ProgramSchedule = schedule,
            ProgramScheduleId = schedule.Id
        };

        await dbContext.Playouts.AddAsync(playout);
        await dbContext.SaveChangesAsync();

        return playout.Id;
    }

    private static async Task<Option<Playout>> GetPlayout(
        TvContext dbContext,
        int playoutId,
        CancellationToken cancellationToken) =>
        await dbContext.Playouts
            .Include(p => p.ProgramScheduleAnchors)
            .ThenInclude(a => a.EnumeratorState)
            .SelectOneAsync(p => p.Id, p => p.Id == playoutId, cancellationToken);

    private static async Task<PlayoutReferenceData> GetReferenceData(
        TvContext dbContext,
        int playoutId,
        PlayoutScheduleKind scheduleKind)
    {
        Channel channel = await dbContext.Channels
            .AsNoTracking()
            .Where(c => c.Playouts.Any(p => p.Id == playoutId))
            .FirstOrDefaultAsync();

        List<PlayoutItem> existingItems = [];
        List<PlayoutTemplate> playoutTemplates = [];

        if (scheduleKind is PlayoutScheduleKind.Block)
        {
            existingItems = await dbContext.PlayoutItems
                .AsNoTracking()
                .Where(pi => pi.PlayoutId == playoutId)
                .ToListAsync();

            playoutTemplates = await dbContext.PlayoutTemplates
                .AsNoTracking()
                .Where(pt => pt.PlayoutId == playoutId)
                .Include(t => t.Template)
                .ThenInclude(t => t.Items)
                .ThenInclude(i => i.Block)
                .ThenInclude(b => b.Items)
                .Include(t => t.DecoTemplate)
                .ThenInclude(t => t.Items)
                .ThenInclude(i => i.Deco)
                .ToListAsync();
        }

        ProgramSchedule programSchedule = await dbContext.ProgramSchedules
            .AsNoTracking()
            .Where(ps => ps.Playouts.Any(p => p.Id == playoutId))
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.ProgramScheduleItemWatermarks)
            .ThenInclude(psi => psi.Watermark)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.ProgramScheduleItemGraphicsElements)
            .ThenInclude(psi => psi.GraphicsElement)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.Collection)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.MediaItem)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.PreRollFiller)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.MidRollFiller)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.PostRollFiller)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.TailFiller)
            .Include(ps => ps.Items)
            .ThenInclude(psi => psi.FallbackFiller)
            .FirstOrDefaultAsync();

        List<ProgramScheduleAlternate> programScheduleAlternates = await dbContext.ProgramScheduleAlternates
            .AsNoTracking()
            .Where(pt => pt.PlayoutId == playoutId)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.ProgramScheduleItemWatermarks)
            .ThenInclude(psi => psi.Watermark)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.ProgramScheduleItemGraphicsElements)
            .ThenInclude(psi => psi.GraphicsElement)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.Collection)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.MediaItem)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.PreRollFiller)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.MidRollFiller)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.PostRollFiller)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.TailFiller)
            .Include(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.FallbackFiller)
            .ToListAsync();

        List<PlayoutHistory> playoutHistory = await dbContext.PlayoutHistory
            .AsNoTracking()
            .Where(h => h.PlayoutId == playoutId)
            .ToListAsync();

        return new PlayoutReferenceData(
            channel,
            Option<Deco>.None,
            existingItems,
            playoutTemplates,
            programSchedule,
            programScheduleAlternates,
            playoutHistory,
            TimeSpan.Zero);
    }
}
