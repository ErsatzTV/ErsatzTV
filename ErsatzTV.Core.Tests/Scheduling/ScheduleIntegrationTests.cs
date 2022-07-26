using Dapper;
using Destructurama;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Data.Repositories;
using ErsatzTV.Infrastructure.Extensions;
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
    public ScheduleIntegrationTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .WriteTo.Console()
            .Destructure.UsingAttributes()
            .CreateLogger();
    }

    [Test]
    public async Task Test()
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

        await dbContext.Libraries.AddAsync(library);
        await dbContext.SaveChangesAsync();
        
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

        await dbContext.Movies.AddRangeAsync(movies);
        await dbContext.SaveChangesAsync();

        var collection = new Collection
        {
            Name = "Test Collection",
            MediaItems = movies.Cast<MediaItem>().ToList()
        };

        await dbContext.Collections.AddAsync(collection);
        await dbContext.SaveChangesAsync();

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
            new MediaCollectionRepository(new Mock<ISearchIndex>().Object, factory),
            new TelevisionRepository(factory),
            new ArtistRepository(factory),
            provider.GetRequiredService<ILogger<PlayoutBuilder>>());

        for (var i = 0; i <= (24 * 4); i++)
        {
            await using TvContext context = await factory.CreateDbContextAsync();
            
            Option<Playout> maybePlayout = await GetPlayout(context, playoutId);
            Playout playout = maybePlayout.ValueUnsafe();

            await builder.Build(playout, PlayoutBuildMode.Continue, start.AddHours(i), finish.AddHours(i));

            await context.SaveChangesAsync();
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
