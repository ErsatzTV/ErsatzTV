using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Threading.Channels;
using Blazored.LocalStorage;
using Dapper;
using ErsatzTV.Application;
using ErsatzTV.Application.Channels.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.GitHub;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Runtime;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Plex;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Formatters;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Data.Repositories;
using ErsatzTV.Infrastructure.GitHub;
using ErsatzTV.Infrastructure.Images;
using ErsatzTV.Infrastructure.Locking;
using ErsatzTV.Infrastructure.Plex;
using ErsatzTV.Infrastructure.Runtime;
using ErsatzTV.Infrastructure.Search;
using ErsatzTV.Serialization;
using ErsatzTV.Services;
using ErsatzTV.Services.RunOnce;
using FluentValidation.AspNetCore;
using Ganss.XSS;
using MediatR;
using MediatR.Courier.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using MudBlazor.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Refit;
using Serilog;

namespace ErsatzTV
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
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

            services.AddSwaggerGen(
                c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "ErsatzTV API", Version = "v1" }); });
            services.AddSwaggerGenNewtonsoftSupport();

            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddMudServices();
            services.AddCourier(Assembly.GetAssembly(typeof(LibraryScanProgress)));
            services.AddBlazoredLocalStorage();

            Log.Logger.Information(
                "ErsatzTV version {Version}",
                Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion ?? "unknown");

            Log.Logger.Warning("This is pre-alpha software and is likely to be unstable");
            Log.Logger.Warning(
                "Give feedback at {GitHub} or {Discord}",
                "https://github.com/jasongdove/ErsatzTV",
                "https://discord.gg/hHaJm3yGy6");

            Log.Logger.Information(
                "Server will listen on port {Port} - try UI at {UI}",
                8409,
                "http://localhost:8409");

            if (!Directory.Exists(FileSystemLayout.AppDataFolder))
            {
                Directory.CreateDirectory(FileSystemLayout.AppDataFolder);
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

            services.AddDbContext<LogContext>(
                options => options.UseSqlite($"Data Source={FileSystemLayout.LogDatabasePath}"));

            services.AddMediatR(typeof(GetAllChannels).Assembly);

            services.AddRefitClient<IPlexTvApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://plex.tv/api/v2"));

            CustomServices(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // app.UseSerilogRequestLogging();

            app.UseSwagger();
            app.UseSwaggerUI(
                c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "ErsatzTV API"); });

            app.UseStaticFiles();

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
            services.AddSingleton<IEntityLocker, EntityLocker>();
            services.AddSingleton<ISearchIndex, SearchIndex>();
            AddChannel<IBackgroundServiceRequest>(services);
            AddChannel<IPlexBackgroundServiceRequest>(services);

            services.AddScoped<IChannelRepository, ChannelRepository>();
            services.AddScoped<IFFmpegProfileRepository, FFmpegProfileRepository>();
            services.AddScoped<IMediaSourceRepository, MediaSourceRepository>();
            services.AddScoped<IMediaItemRepository, MediaItemRepository>();
            services.AddScoped<IMediaCollectionRepository, MediaCollectionRepository>();
            services.AddScoped<IResolutionRepository, ResolutionRepository>();
            services.AddScoped<IConfigElementRepository, ConfigElementRepository>();
            services.AddScoped<IProgramScheduleRepository, ProgramScheduleRepository>();
            services.AddScoped<IPlayoutRepository, PlayoutRepository>();
            services.AddScoped<ILogRepository, LogRepository>();
            services.AddScoped<ITelevisionRepository, TelevisionRepository>();
            services.AddScoped<ISearchRepository, SearchRepository>();
            services.AddScoped<IMovieRepository, MovieRepository>();
            services.AddScoped<IArtistRepository, ArtistRepository>();
            services.AddScoped<IMusicVideoRepository, MusicVideoRepository>();
            services.AddScoped<ILibraryRepository, LibraryRepository>();
            services.AddScoped<IMetadataRepository, MetadataRepository>();
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
            services.AddScoped<IPlexMovieLibraryScanner, PlexMovieLibraryScanner>();
            services.AddScoped<IPlexTelevisionLibraryScanner, PlexTelevisionLibraryScanner>();
            services.AddScoped<IPlexServerApiClient, PlexServerApiClient>();
            services.AddScoped<IRuntimeInfo, RuntimeInfo>();
            services.AddScoped<IPlexPathReplacementService, PlexPathReplacementService>();
            services.AddScoped<IFFmpegStreamSelector, FFmpegStreamSelector>();
            services.AddScoped<FFmpegProcessService>();
            services.AddScoped<IGitHubApiClient, GitHubApiClient>();
            services.AddScoped<IHtmlSanitizer, HtmlSanitizer>(
                _ =>
                {
                    var sanitizer = new HtmlSanitizer();
                    sanitizer.AllowedAttributes.Add("class");
                    return sanitizer;
                });

            services.AddHostedService<DatabaseMigratorService>();
            services.AddHostedService<CacheCleanerService>();
            services.AddHostedService<PlexService>();
            services.AddHostedService<FFmpegLocatorService>();
            services.AddHostedService<WorkerService>();
            services.AddHostedService<SchedulerService>();
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
}
