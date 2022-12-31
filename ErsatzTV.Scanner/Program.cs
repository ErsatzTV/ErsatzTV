﻿using Bugsnag;
using Bugsnag.Payload;
using ErsatzTV.Core;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Metadata.Nfo;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Repositories.Caching;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Metadata.Nfo;
using ErsatzTV.Core.Plex;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Data.Repositories;
using ErsatzTV.Infrastructure.Data.Repositories.Caching;
using ErsatzTV.Infrastructure.Emby;
using ErsatzTV.Infrastructure.Images;
using ErsatzTV.Infrastructure.Jellyfin;
using ErsatzTV.Infrastructure.Plex;
using ErsatzTV.Infrastructure.Runtime;
using ErsatzTV.Infrastructure.Search;
using ErsatzTV.Scanner.Core.Emby;
using ErsatzTV.Scanner.Core.FFmpeg;
using ErsatzTV.Scanner.Core.Interfaces.FFmpeg;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using ErsatzTV.Scanner.Core.Jellyfin;
using ErsatzTV.Scanner.Core.Metadata;
using ErsatzTV.Scanner.Core.Plex;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IO;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Exception = System.Exception;

namespace ErsatzTV.Scanner;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .Enrich.FromLogContext()
            .WriteTo.Console(new CompactJsonFormatter(), standardErrorFromLevel: LogEventLevel.Debug)
            .CreateLogger();
        
        try
        {
            await CreateHostBuilder(args).Build().RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "ErsatzTV.Scanner host terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(
                (_, services) =>
                {
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

                    services.AddScoped<IConfigElementRepository, ConfigElementRepository>();
                    services.AddScoped<IMetadataRepository, MetadataRepository>();
                    services.AddScoped<IMediaSourceRepository, MediaSourceRepository>();
                    services.AddScoped<IMediaItemRepository, MediaItemRepository>();
                    services.AddScoped<IMovieRepository, MovieRepository>();
                    services.AddScoped<ITelevisionRepository, TelevisionRepository>();
                    services.AddScoped<IArtistRepository, ArtistRepository>();
                    services.AddScoped<IMusicVideoRepository, MusicVideoRepository>();
                    services.AddScoped<IOtherVideoRepository, OtherVideoRepository>();
                    services.AddScoped<ISongRepository, SongRepository>();
                    services.AddScoped<ILibraryRepository, LibraryRepository>();
                    services.AddScoped<ISearchRepository, SearchRepository>();
                    services.AddScoped<ICachingSearchRepository, CachingSearchRepository>();
                    services.AddScoped<ILocalMetadataProvider, LocalMetadataProvider>();
                    services.AddScoped<IFallbackMetadataProvider, FallbackMetadataProvider>();
                    services.AddScoped<ILocalStatisticsProvider, LocalStatisticsProvider>();
                    services.AddScoped<ILocalSubtitlesProvider, LocalSubtitlesProvider>();
                    services.AddScoped<IImageCache, ImageCache>();
                    services.AddScoped<ILocalFileSystem, LocalFileSystem>();
                    services.AddScoped<IMovieFolderScanner, MovieFolderScanner>();
                    services.AddScoped<ITelevisionFolderScanner, TelevisionFolderScanner>();
                    services.AddScoped<IMusicVideoFolderScanner, MusicVideoFolderScanner>();
                    services.AddScoped<IOtherVideoFolderScanner, OtherVideoFolderScanner>();
                    services.AddScoped<ISongFolderScanner, SongFolderScanner>();
                    services.AddScoped<IEpisodeNfoReader, EpisodeNfoReader>();
                    services.AddScoped<IMovieNfoReader, MovieNfoReader>();
                    services.AddScoped<IArtistNfoReader, ArtistNfoReader>();
                    services.AddScoped<IMusicVideoNfoReader, MusicVideoNfoReader>();
                    services.AddScoped<ITvShowNfoReader, TvShowNfoReader>();
                    services.AddScoped<IOtherVideoNfoReader, OtherVideoNfoReader>();
                    services.AddScoped<IFFmpegPngService, FFmpegPngService>();
                    services.AddScoped<IRuntimeInfo, RuntimeInfo>();
                    
                    services.AddScoped<IPlexMovieLibraryScanner, PlexMovieLibraryScanner>();
                    services.AddScoped<IPlexTelevisionLibraryScanner, PlexTelevisionLibraryScanner>();
                    services.AddScoped<IPlexServerApiClient, PlexServerApiClient>();
                    services.AddScoped<IPlexMovieRepository, PlexMovieRepository>();
                    services.AddScoped<IPlexTelevisionRepository, PlexTelevisionRepository>();
                    services.AddScoped<IPlexPathReplacementService, PlexPathReplacementService>();
                    services.AddScoped<PlexEtag>();
                    
                    services.AddScoped<IEmbyMovieLibraryScanner, EmbyMovieLibraryScanner>();
                    services.AddScoped<IEmbyTelevisionLibraryScanner, EmbyTelevisionLibraryScanner>();
                    services.AddScoped<IEmbyCollectionScanner, EmbyCollectionScanner>();
                    services.AddScoped<IEmbyApiClient, EmbyApiClient>();
                    services.AddScoped<IEmbyCollectionRepository, EmbyCollectionRepository>();
                    services.AddScoped<IEmbyMovieRepository, EmbyMovieRepository>();
                    services.AddScoped<IEmbyTelevisionRepository, EmbyTelevisionRepository>();
                    services.AddScoped<IEmbyPathReplacementService, EmbyPathReplacementService>();

                    services.AddScoped<IJellyfinMovieLibraryScanner, JellyfinMovieLibraryScanner>();
                    services.AddScoped<IJellyfinTelevisionLibraryScanner, JellyfinTelevisionLibraryScanner>();
                    services.AddScoped<IJellyfinCollectionScanner, JellyfinCollectionScanner>();
                    services.AddScoped<IJellyfinApiClient, JellyfinApiClient>();
                    services.AddScoped<IJellyfinCollectionRepository, JellyfinCollectionRepository>();
                    services.AddScoped<IJellyfinMovieRepository, JellyfinMovieRepository>();
                    services.AddScoped<IJellyfinTelevisionRepository, JellyfinTelevisionRepository>();
                    services.AddScoped<IJellyfinPathReplacementService, JellyfinPathReplacementService>();
                    
                    services.AddSingleton<ITempFilePool, TempFilePool>();
                    services.AddSingleton<IPlexSecretStore, PlexSecretStore>();
                    services.AddSingleton<IEmbySecretStore, EmbySecretStore>();
                    services.AddSingleton<IJellyfinSecretStore, JellyfinSecretStore>();
                    services.AddSingleton<ISearchIndex, SearchIndex>();
                    services.AddSingleton<RecyclableMemoryStreamManager>();
                    // TODO: real bugsnag?
                    services.AddSingleton<IClient>(_ => new BugsnagNoopClient());
                    
                    services.AddMediatR(typeof(Worker).Assembly);
                    services.AddMemoryCache();
                    
                    services.AddHostedService<Worker>();
                })
            .UseSerilog();

    private class BugsnagNoopClient : IClient
    {
        public void Notify(Exception exception) { }
        public void Notify(Exception exception, Middleware callback) { }
        public void Notify(Exception exception, Severity severity) { }
        public void Notify(Exception exception, Severity severity, Middleware callback) { }
        public void Notify(Exception exception, HandledState handledState) { }
        public void Notify(Exception exception, HandledState handledState, Middleware callback) { }
        public void Notify(Report report, Middleware callback) { }
        public void BeforeNotify(Middleware middleware) { }

        public IBreadcrumbs Breadcrumbs => new Breadcrumbs(Configuration);
        public ISessionTracker SessionTracking => new SessionTracker(Configuration);
        public IConfiguration Configuration => new Configuration();
    }
}