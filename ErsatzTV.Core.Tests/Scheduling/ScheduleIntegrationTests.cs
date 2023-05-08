using Bugsnag;
using Dapper;
using Destructurama;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Repositories.Caching;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Data.Repositories;
using ErsatzTV.Infrastructure.Data.Repositories.Caching;
using ErsatzTV.Infrastructure.Extensions;
using ErsatzTV.Infrastructure.Search;
using LanguageExt.UnsafeValueAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace ErsatzTV.Core.Tests.Scheduling;

[TestFixture]
[Explicit]
public class ScheduleIntegrationTests
{
    private CancellationToken _cancellationToken;

    public ScheduleIntegrationTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .WriteTo.Console()
            .Destructure.UsingAttributes()
            .CreateLogger();
    }
    
    [SetUp]
    public void SetUp()
    {
        _cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;
    }

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
                    o.MigrationsAssembly("ErsatzTV.Infrastructure");
                }),
            ServiceLifetime.Scoped,
            ServiceLifetime.Singleton);

        services.AddDbContextFactory<TvContext>(
            options => options.UseSqlite(
                connectionString,
                o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    o.MigrationsAssembly("ErsatzTV.Infrastructure");
                }));
        
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());

        services.AddSingleton((Func<IServiceProvider, ILoggerFactory>)(_ => new SerilogLoggerFactory()));
        
        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<ICachingSearchRepository, CachingSearchRepository>();
        services.AddScoped<IConfigElementRepository, ConfigElementRepository>();
        services.AddScoped<IFallbackMetadataProvider, FallbackMetadataProvider>();

        services.AddSingleton<ISearchIndex, SearchIndex>();

        services.AddSingleton(_ => new Mock<IClient>().Object);

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
            provider.GetRequiredService<IConfigElementRepository>());

        await searchIndex.Rebuild(
            provider.GetRequiredService<ICachingSearchRepository>(),
            provider.GetRequiredService<IFallbackMetadataProvider>());
        
        var builder = new PlayoutBuilder(
            new ConfigElementRepository(factory),
            new MediaCollectionRepository(new Mock<IClient>().Object, searchIndex, factory),
            new TelevisionRepository(factory, provider.GetRequiredService<ILogger<TelevisionRepository>>()),
            new ArtistRepository(factory),
            new Mock<IMultiEpisodeShuffleCollectionEnumeratorFactory>().Object,
            new Mock<ILocalFileSystem>().Object,
            provider.GetRequiredService<ILogger<PlayoutBuilder>>());

        {
            await using TvContext context = await factory.CreateDbContextAsync(_cancellationToken);

            Option<Playout> maybePlayout = await GetPlayout(context, PLAYOUT_ID);
            Playout playout = maybePlayout.ValueUnsafe();

            await builder.Build(playout, PlayoutBuildMode.Reset, start, finish, _cancellationToken);

            await context.SaveChangesAsync(_cancellationToken);
        }

        for (var i = 1; i <= (24 * 1); i++)
        {
            await using TvContext context = await factory.CreateDbContextAsync(_cancellationToken);
            
            Option<Playout> maybePlayout = await GetPlayout(context, PLAYOUT_ID);
            Playout playout = maybePlayout.ValueUnsafe();

            await builder.Build(
                playout,
                PlayoutBuildMode.Continue,
                start.AddHours(i),
                finish.AddHours(i),
                _cancellationToken);

            await context.SaveChangesAsync(_cancellationToken);
        }
        
        for (var i = 25; i <= 26; i++)
        {
            await using TvContext context = await factory.CreateDbContextAsync(_cancellationToken);
            
            Option<Playout> maybePlayout = await GetPlayout(context, PLAYOUT_ID);
            Playout playout = maybePlayout.ValueUnsafe();

            await builder.Build(
                playout,
                PlayoutBuildMode.Continue,
                start.AddHours(i),
                finish.AddHours(i),
                _cancellationToken);

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
                    o.MigrationsAssembly("ErsatzTV.Infrastructure");
                }),
            ServiceLifetime.Scoped,
            ServiceLifetime.Singleton);

        services.AddDbContextFactory<TvContext>(
            options => options.UseSqlite(
                connectionString,
                o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    o.MigrationsAssembly("ErsatzTV.Infrastructure");
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

        DateTimeOffset start = new DateTimeOffset(2022, 7, 26, 8, 0, 5, TimeSpan.FromHours(-5));
        DateTimeOffset finish = start.AddDays(2);
        
        var builder = new PlayoutBuilder(
            new ConfigElementRepository(factory),
            new MediaCollectionRepository(new Mock<IClient>().Object, new Mock<ISearchIndex>().Object, factory),
            new TelevisionRepository(factory, provider.GetRequiredService<ILogger<TelevisionRepository>>()),
            new ArtistRepository(factory),
            new Mock<IMultiEpisodeShuffleCollectionEnumeratorFactory>().Object,
            new Mock<ILocalFileSystem>().Object,
            provider.GetRequiredService<ILogger<PlayoutBuilder>>());

        for (var i = 0; i <= (24 * 4); i++)
        {
            await using TvContext context = await factory.CreateDbContextAsync(_cancellationToken);
            
            Option<Playout> maybePlayout = await GetPlayout(context, playoutId);
            Playout playout = maybePlayout.ValueUnsafe();

            await builder.Build(
                playout,
                PlayoutBuildMode.Continue,
                start.AddHours(i),
                finish.AddHours(i),
                _cancellationToken);

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

    private static async Task<Option<Playout>> GetPlayout(TvContext dbContext, int playoutId)
    {
        return await dbContext.Playouts
            .Include(p => p.Channel)
            .Include(p => p.Items)
            
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.Collection)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.MediaItem)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.PreRollFiller)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.MidRollFiller)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.PostRollFiller)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.TailFiller)
            .Include(p => p.ProgramScheduleAlternates)
            .ThenInclude(a => a.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.FallbackFiller)
            
            .Include(p => p.ProgramScheduleAnchors)
            .ThenInclude(a => a.EnumeratorState)
            .Include(p => p.ProgramScheduleAnchors)
            .ThenInclude(a => a.MediaItem)
            
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.Collection)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.MediaItem)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.PreRollFiller)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.MidRollFiller)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.PostRollFiller)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.TailFiller)
            .Include(p => p.ProgramSchedule)
            .ThenInclude(ps => ps.Items)
            .ThenInclude(psi => psi.FallbackFiller)
            .SelectOneAsync(p => p.Id, p => p.Id == playoutId);
    }
}
