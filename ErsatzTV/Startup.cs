using System.Data;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using Bugsnag.AspNet.Core;
using Dapper;
using ErsatzTV.Application;
using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Streaming;
using ErsatzTV.Core;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.GitHub;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Runtime;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Interfaces.Trakt;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Metadata.Nfo;
using ErsatzTV.Core.Plex;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Core.Trakt;
using ErsatzTV.Formatters;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Data.Repositories;
using ErsatzTV.Infrastructure.Emby;
using ErsatzTV.Infrastructure.GitHub;
using ErsatzTV.Infrastructure.Health;
using ErsatzTV.Infrastructure.Health.Checks;
using ErsatzTV.Infrastructure.Images;
using ErsatzTV.Infrastructure.Jellyfin;
using ErsatzTV.Infrastructure.Locking;
using ErsatzTV.Infrastructure.Plex;
using ErsatzTV.Infrastructure.Runtime;
using ErsatzTV.Infrastructure.Search;
using ErsatzTV.Infrastructure.Trakt;
using ErsatzTV.Serialization;
using ErsatzTV.Services;
using ErsatzTV.Services.RunOnce;
using FluentValidation.AspNetCore;
using Ganss.XSS;
using MediatR;
using MediatR.Courier.DependencyInjection;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Refit;
using Serilog;

namespace ErsatzTV;

public class Startup
{
    public Startup(IConfiguration configuration) => Configuration = configuration;

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        BugsnagConfiguration bugsnagConfig = Configuration.GetSection("Bugsnag").Get<BugsnagConfiguration>();
        services.Configure<BugsnagConfiguration>(Configuration.GetSection("Bugsnag"));

        services.AddBugsnag(
            configuration =>
            {
                configuration.ApiKey = bugsnagConfig.ApiKey;
                configuration.ProjectNamespaces = new[] { "ErsatzTV" };
                configuration.AppVersion = Assembly.GetEntryAssembly()
                    ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion ?? "unknown";
                    
                configuration.NotifyReleaseStages = new[] { "public", "develop" };
                    
#if DEBUG
                configuration.ReleaseStage = "develop";
#else
                    // effectively "disable" by tweaking app config
                    configuration.ReleaseStage = bugsnagConfig.Enable ? "public" : "private";
#endif
            });
            
        services.AddCors(
            o => o.AddPolicy(
                "AllowAll",
                builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                }));

        services.AddControllers(
                options =>
                {
                    options.OutputFormatters.Insert(0, new ConcatPlaylistOutputFormatter());
                    options.OutputFormatters.Insert(0, new ChannelPlaylistOutputFormatter());
                    options.OutputFormatters.Insert(0, new ChannelGuideOutputFormatter());
                    options.OutputFormatters.Insert(0, new DeviceXmlOutputFormatter());
                    options.OutputFormatters.Insert(0, new HdhrJsonOutputFormatter());
                })
            .AddNewtonsoftJson(
                opt =>
                {
                    opt.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    opt.SerializerSettings.ContractResolver = new CustomContractResolver();
                    opt.SerializerSettings.Converters.Add(new StringEnumConverter());
                })
            .AddFluentValidation(
                options =>
                {
                    options.RegisterValidatorsFromAssemblyContaining<Startup>();
                    options.ImplicitlyValidateChildProperties = true;
                });

        services.AddRazorPages();
        services.AddServerSideBlazor();

        services.AddMudServices();

        var coreAssembly = Assembly.GetAssembly(typeof(LibraryScanProgress));
        if (coreAssembly != null)
        {
            services.AddCourier(coreAssembly);
        }

        Console.OutputEncoding = Encoding.UTF8;

        Log.Logger.Information(
            "ErsatzTV version {Version}",
            Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown");

        Log.Logger.Warning("This is alpha software and is likely to be unstable");
        Log.Logger.Warning(
            "Give feedback at {GitHub} or {Discord}",
            "https://github.com/jasongdove/ErsatzTV",
            "https://discord.gg/hHaJm3yGy6");

        if (!Directory.Exists(FileSystemLayout.AppDataFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.AppDataFolder);
        }

        if (!Directory.Exists(FileSystemLayout.TranscodeFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.TranscodeFolder);
        }

        if (!Directory.Exists(FileSystemLayout.TempFilePoolFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.TempFilePoolFolder);
        }

        Log.Logger.Information("Database is at {DatabasePath}", FileSystemLayout.DatabasePath);

        // until we add a setting for a file-specific scheme://host:port to access
        // stream urls contained in this file, it doesn't make sense to do
        // for now, continue to use scheme and host from incoming requests
        // string xmltvPath = Path.Combine(appDataFolder, "xmltv.xml");
        // Log.Logger.Information("XMLTV is at {XmltvPath}", xmltvPath);

        var connectionString = $"Data Source={FileSystemLayout.DatabasePath};foreign keys=true;";

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
            
        services.AddTransient<IDbConnection>(_ => new SqliteConnection(connectionString));
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());

        var logConnectionString = $"Data Source={FileSystemLayout.LogDatabasePath}";
            
        services.AddDbContext<LogContext>(
            options => options.UseSqlite(logConnectionString),
            ServiceLifetime.Scoped,
            ServiceLifetime.Singleton);

        services.AddDbContextFactory<LogContext>(
            options => options.UseSqlite(logConnectionString));

        services.AddMediatR(typeof(GetAllChannels).Assembly);

        services.AddRefitClient<IPlexTvApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://plex.tv/api/v2"));

        services.AddRefitClient<ITraktApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.trakt.tv"));

        services.Configure<TraktConfiguration>(Configuration.GetSection("Trakt"));

        CustomServices(services);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCors("AllowAll");

        // app.UseSerilogRequestLogging();

        app.UseStaticFiles();

        var extensionProvider = new FileExtensionContentTypeProvider();
        extensionProvider.Mappings.Add(".m3u8", "application/vnd.apple.mpegurl");

        app.UseStaticFiles(
            new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(FileSystemLayout.TranscodeFolder),
                RequestPath = "/iptv/session",
                ContentTypeProvider = extensionProvider,
                OnPrepareResponse = ctx =>
                {
                    // Log.Logger.Information("Transcode access: {Test}", ctx.File.PhysicalPath);
                    ChannelWriter<IFFmpegWorkerRequest> writer = app.ApplicationServices
                        .GetRequiredService<ChannelWriter<IFFmpegWorkerRequest>>();
                    writer.TryWrite(new TouchFFmpegSession(ctx.File.PhysicalPath));
                }
            });

        app.UseRouting();

        app.UseEndpoints(
            endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
    }

    private void CustomServices(IServiceCollection services)
    {
        services.AddSingleton<FFmpegPlaybackSettingsCalculator>();
        services.AddSingleton<IPlexSecretStore, PlexSecretStore>();
        services.AddSingleton<IPlexTvApiClient, PlexTvApiClient>(); // TODO: does this need to be singleton?
        services.AddSingleton<ITraktApiClient, TraktApiClient>();
        services.AddSingleton<IEntityLocker, EntityLocker>();
        services.AddSingleton<ISearchIndex, SearchIndex>();
        services.AddSingleton<IFFmpegSegmenterService, FFmpegSegmenterService>();
        services.AddSingleton<ITempFilePool, TempFilePool>();
        services.AddSingleton<IHlsPlaylistFilter, HlsPlaylistFilter>();
        AddChannel<IBackgroundServiceRequest>(services);
        AddChannel<IPlexBackgroundServiceRequest>(services);
        AddChannel<IJellyfinBackgroundServiceRequest>(services);
        AddChannel<IEmbyBackgroundServiceRequest>(services);
        AddChannel<IFFmpegWorkerRequest>(services);
            
        services.AddScoped<IFFmpegVersionHealthCheck, FFmpegVersionHealthCheck>();
        services.AddScoped<IFFmpegReportsHealthCheck, FFmpegReportsHealthCheck>();
        services.AddScoped<IHardwareAccelerationHealthCheck, HardwareAccelerationHealthCheck>();
        services.AddScoped<IMovieMetadataHealthCheck, MovieMetadataHealthCheck>();
        services.AddScoped<IEpisodeMetadataHealthCheck, EpisodeMetadataHealthCheck>();
        services.AddScoped<IZeroDurationHealthCheck, ZeroDurationHealthCheck>();
        services.AddScoped<IFileNotFoundHealthCheck, FileNotFoundHealthCheck>();
        services.AddScoped<IVaapiDriverHealthCheck, VaapiDriverHealthCheck>();
        services.AddScoped<IErrorReportsHealthCheck, ErrorReportsHealthCheck>();
        services.AddScoped<IHealthCheckService, HealthCheckService>();

        services.AddScoped<IChannelRepository, ChannelRepository>();
        services.AddScoped<IFFmpegProfileRepository, FFmpegProfileRepository>();
        services.AddScoped<IMediaSourceRepository, MediaSourceRepository>();
        services.AddScoped<IMediaItemRepository, MediaItemRepository>();
        services.AddScoped<IMediaCollectionRepository, MediaCollectionRepository>();
        services.AddScoped<IConfigElementRepository, ConfigElementRepository>();
        services.AddScoped<ITelevisionRepository, TelevisionRepository>();
        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<IMovieRepository, MovieRepository>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IMusicVideoRepository, MusicVideoRepository>();
        services.AddScoped<IOtherVideoRepository, OtherVideoRepository>();
        services.AddScoped<ISongRepository, SongRepository>();
        services.AddScoped<ILibraryRepository, LibraryRepository>();
        services.AddScoped<IMetadataRepository, MetadataRepository>();
        services.AddScoped<IArtworkRepository, ArtworkRepository>();
        services.AddScoped<IFFmpegLocator, FFmpegLocator>();
        services.AddScoped<ILocalMetadataProvider, LocalMetadataProvider>();
        services.AddScoped<IFallbackMetadataProvider, FallbackMetadataProvider>();
        services.AddScoped<ILocalStatisticsProvider, LocalStatisticsProvider>();
        services.AddScoped<IPlayoutBuilder, PlayoutBuilder>();
        services.AddScoped<IImageCache, ImageCache>();
        services.AddScoped<ILocalFileSystem, LocalFileSystem>();
        services.AddScoped<IMovieFolderScanner, MovieFolderScanner>();
        services.AddScoped<ITelevisionFolderScanner, TelevisionFolderScanner>();
        services.AddScoped<IMusicVideoFolderScanner, MusicVideoFolderScanner>();
        services.AddScoped<IOtherVideoFolderScanner, OtherVideoFolderScanner>();
        services.AddScoped<ISongFolderScanner, SongFolderScanner>();
        services.AddScoped<IPlexMovieLibraryScanner, PlexMovieLibraryScanner>();
        services.AddScoped<IPlexTelevisionLibraryScanner, PlexTelevisionLibraryScanner>();
        services.AddScoped<IPlexServerApiClient, PlexServerApiClient>();
        services.AddScoped<IJellyfinMovieLibraryScanner, JellyfinMovieLibraryScanner>();
        services.AddScoped<IJellyfinTelevisionLibraryScanner, JellyfinTelevisionLibraryScanner>();
        services.AddScoped<IJellyfinApiClient, JellyfinApiClient>();
        services.AddScoped<IJellyfinPathReplacementService, JellyfinPathReplacementService>();
        services.AddScoped<IJellyfinTelevisionRepository, JellyfinTelevisionRepository>();
        services.AddScoped<IEmbyApiClient, EmbyApiClient>();
        services.AddScoped<IEmbyMovieLibraryScanner, EmbyMovieLibraryScanner>();
        services.AddScoped<IEmbyTelevisionLibraryScanner, EmbyTelevisionLibraryScanner>();
        services.AddScoped<IEmbyPathReplacementService, EmbyPathReplacementService>();
        services.AddScoped<IEmbyTelevisionRepository, EmbyTelevisionRepository>();
        services.AddScoped<IRuntimeInfo, RuntimeInfo>();
        services.AddScoped<IPlexPathReplacementService, PlexPathReplacementService>();
        services.AddScoped<IFFmpegStreamSelector, FFmpegStreamSelector>();
            
        // services.AddScoped<IFFmpegProcessService, FFmpegProcessService>();
        services.AddScoped<IFFmpegProcessServiceFactory, FFmpegProcessServiceFactory>();
        services.AddScoped<FFmpegLibraryProcessService>();
        services.AddScoped<FFmpegProcessService>();
            
        services.AddScoped<ISongVideoGenerator, SongVideoGenerator>();
        services.AddScoped<HlsSessionWorker>();
        services.AddScoped<IGitHubApiClient, GitHubApiClient>();
        services.AddScoped<IHtmlSanitizer, HtmlSanitizer>(
            _ =>
            {
                var sanitizer = new HtmlSanitizer();
                sanitizer.AllowedAttributes.Add("class");
                return sanitizer;
            });
        services.AddScoped<IJellyfinSecretStore, JellyfinSecretStore>();
        services.AddScoped<IEmbySecretStore, EmbySecretStore>();
        services.AddScoped<IEpisodeNfoReader, EpisodeNfoReader>();

        // services.AddTransient(typeof(IRequestHandler<,>), typeof(GetRecentLogEntriesHandler<>));

        services.AddHostedService<EndpointValidatorService>();
        services.AddHostedService<DatabaseMigratorService>();
        services.AddHostedService<CacheCleanerService>();
        services.AddHostedService<ResourceExtractorService>();
        services.AddHostedService<PlatformSettingsService>();
        services.AddHostedService<EmbyService>();
        services.AddHostedService<JellyfinService>();
        services.AddHostedService<PlexService>();
        services.AddHostedService<FFmpegLocatorService>();
        services.AddHostedService<WorkerService>();
        services.AddHostedService<SchedulerService>();
        services.AddHostedService<FFmpegWorkerService>();
    }

    private void AddChannel<TMessageType>(IServiceCollection services)
    {
        services.AddSingleton(
            Channel.CreateUnbounded<TMessageType>(new UnboundedChannelOptions { SingleReader = true }));
        services.AddSingleton(
            provider => provider.GetRequiredService<Channel<TMessageType>>().Reader);
        services.AddSingleton(
            provider => provider.GetRequiredService<Channel<TMessageType>>().Writer);
    }
}