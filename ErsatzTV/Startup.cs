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
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Repositories.Caching;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Interfaces.Scripting;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Interfaces.Trakt;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Plex;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Core.Trakt;
using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Pipeline;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Formatters;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Data.Repositories;
using ErsatzTV.Infrastructure.Data.Repositories.Caching;
using ErsatzTV.Infrastructure.Emby;
using ErsatzTV.Infrastructure.FFmpeg;
using ErsatzTV.Infrastructure.GitHub;
using ErsatzTV.Infrastructure.Health;
using ErsatzTV.Infrastructure.Health.Checks;
using ErsatzTV.Infrastructure.Images;
using ErsatzTV.Infrastructure.Jellyfin;
using ErsatzTV.Infrastructure.Locking;
using ErsatzTV.Infrastructure.Plex;
using ErsatzTV.Infrastructure.Runtime;
using ErsatzTV.Infrastructure.Scheduling;
using ErsatzTV.Infrastructure.Scripting;
using ErsatzTV.Infrastructure.Search;
using ErsatzTV.Infrastructure.Trakt;
using ErsatzTV.Serialization;
using ErsatzTV.Services;
using ErsatzTV.Services.RunOnce;
using FluentValidation;
using FluentValidation.AspNetCore;
using Ganss.Xss;
using MediatR.Courier.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IO;
using MudBlazor.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Refit;
using Serilog;

namespace ErsatzTV;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        CurrentEnvironment = env;
    }

    public IConfiguration Configuration { get; }

    private IWebHostEnvironment CurrentEnvironment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        BugsnagConfiguration bugsnagConfig = Configuration.GetSection("Bugsnag").Get<BugsnagConfiguration>();
        services.Configure<BugsnagConfiguration>(Configuration.GetSection("Bugsnag"));
        services.Configure<ForwardedHeadersOptions>(
            options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;
                options.ForwardLimit = 2;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

        services.AddBugsnag(
            configuration =>
            {
                configuration.ApiKey = bugsnagConfig.ApiKey;
                configuration.ProjectNamespaces = new[] { "ErsatzTV" };
                configuration.AppVersion = Assembly.GetEntryAssembly()
                    ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion ?? "unknown";
                configuration.AutoNotify = true;

                configuration.NotifyReleaseStages = new[] { "public", "develop" };

#if DEBUG || DEBUG_NO_SYNC
                configuration.ReleaseStage = "develop";
#else
                    // effectively "disable" by tweaking app config
                    configuration.ReleaseStage = bugsnagConfig.Enable ? "public" : "private";
#endif
            });

        OidcHelper.Init(Configuration);
        JwtHelper.Init(Configuration);

        if (OidcHelper.IsEnabled)
        {
            services.AddAuthentication(
                    options =>
                    {
                        options.DefaultScheme = "cookie";
                        options.DefaultChallengeScheme = "oidc";
                    })
                .AddCookie(
                    "cookie",
                    options =>
                    {
                        options.CookieManager = new ChunkingCookieManager();

                        options.Cookie.HttpOnly = true;
                        options.Cookie.SameSite = SameSiteMode.None;
                        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    })
                .AddOpenIdConnect(
                    "oidc",
                    options =>
                    {
                        options.Authority = OidcHelper.Authority;
                        options.ClientId = OidcHelper.ClientId;
                        options.ClientSecret = OidcHelper.ClientSecret;

                        options.ResponseType = OpenIdConnectResponseType.Code;
                        options.UsePkce = true;
                        options.ResponseMode = OpenIdConnectResponseMode.Query;

                        options.Scope.Clear();
                        options.Scope.Add("openid");

                        options.CallbackPath = new PathString("/callback");

                        options.SaveTokens = true;

                        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
                        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

                        if (!string.IsNullOrWhiteSpace(OidcHelper.LogoutUri))
                        {
                            options.Events = new OpenIdConnectEvents
                            {
                                OnRedirectToIdentityProviderForSignOut = context =>
                                {
                                    context.Response.Redirect(OidcHelper.LogoutUri);
                                    context.HandleResponse();

                                    return Task.CompletedTask;
                                }
                            };
                        }
                    })
                .AddJwtBearer(
                "jwt",
                options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = JwtHelper.IssuerSigningKey,
                        ValidateLifetime = true
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = static context =>
                        {
                            if (context.Request.Query.TryGetValue("access_token", out var token))
                                context.Token = token;
                            return Task.CompletedTask;
                        }
                    };
                });
            services.AddAuthorization(
                options =>
                {
                    var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder(
                        "cookie",
                        "oidc");

                    defaultAuthorizationPolicyBuilder = defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();

                    options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();

                    var onlyJwtSchemePolicyBuilder = new AuthorizationPolicyBuilder("jwt");
                    if (JwtHelper.IsEnabled)
                    {
                        options.AddPolicy("JwtOnlyScheme", onlyJwtSchemePolicyBuilder
                        .RequireAuthenticatedUser()
                        .Build());
                    }
                    else
                    {
                        options.AddPolicy("JwtOnlyScheme", onlyJwtSchemePolicyBuilder
                        .RequireAssertion(_ => true)
                        .Build());
                    }

                }
                );
        }


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
                    });

        services.AddScoped(_ => new ConditionalAuthorizeFilter("JwtOnlyScheme"));

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Startup>();

        string v2 = Environment.GetEnvironmentVariable("ETV_UI_V2");
        if (!CurrentEnvironment.IsDevelopment() && !string.IsNullOrWhiteSpace(v2))
        {
            services.AddSpaStaticFiles(options => options.RootPath = "wwwroot/v2");
        }

        services.AddMemoryCache();

        services.AddRazorPages(
            options =>
            {
                if (OidcHelper.IsEnabled)
                {
                    options.Conventions.AuthorizeFolder("/");
                }
            });

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

        Log.Logger.Warning("This is beta software and may be unstable");
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

        if (!Directory.Exists(FileSystemLayout.FontsCacheFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.FontsCacheFolder);
        }

        if (!Directory.Exists(FileSystemLayout.TemplatesFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.TemplatesFolder);
        }

        if (!Directory.Exists(FileSystemLayout.MusicVideoCreditsTemplatesFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.MusicVideoCreditsTemplatesFolder);
        }

        if (!Directory.Exists(FileSystemLayout.ScriptsFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.ScriptsFolder);
        }

        if (!Directory.Exists(FileSystemLayout.MultiEpisodeShuffleTemplatesFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.MultiEpisodeShuffleTemplatesFolder);
        }

        if (!Directory.Exists(FileSystemLayout.AudioStreamSelectorScriptsFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.AudioStreamSelectorScriptsFolder);
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

        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());

        services.AddMediatR(config => config.RegisterServicesFromAssemblyContaining<GetAllChannels>());

        services.AddRefitClient<IPlexTvApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://plex.tv/api/v2"));

        services.AddRefitClient<ITraktApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.trakt.tv"));

        services.Configure<TraktConfiguration>(Configuration.GetSection("Trakt"));

        CustomServices(services);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        string baseUrl = Environment.GetEnvironmentVariable("ETV_BASE_URL");
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            try
            {
                app.UsePathBase(baseUrl);
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "Failed to configure ETV_BASE_URL; please check syntax and include leading slash e.g. `/etv`: {BaseUrl}",
                    baseUrl);
            }
        }

        app.UseCors("AllowAll");
        app.UseForwardedHeaders();

        // app.UseHttpLogging();
        // app.UseSerilogRequestLogging();

        app.UseStaticFiles();

        var extensionProvider = new FileExtensionContentTypeProvider();
        extensionProvider.Mappings.Add(".m3u8", "application/x-mpegurl");

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

        if (OidcHelper.IsEnabled)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        app.Use(async (context, next) =>
        {
            if (JwtHelper.IsEnabled)
            {
                // Only apply the [Authorize] tag to the IptvController
                if (context.Request.Path.StartsWithSegments("/Iptv"))
                {
                    var authPolicyBuilder = new AuthorizationPolicyBuilder();
                    authPolicyBuilder.RequireAuthenticatedUser();
                    authPolicyBuilder.AddAuthenticationSchemes("JwtOnlyScheme");

                    var authPolicy = authPolicyBuilder.Build();
                    var authFilter = new AuthorizeFilter(authPolicy);

                    context.Items["authFilter"] = authFilter;
                }
            }

            await next();
        });


        string v2 = Environment.GetEnvironmentVariable("ETV_UI_V2");
        if (!env.IsDevelopment() && !string.IsNullOrWhiteSpace(v2))
        {
            app.Map(
                "/v2",
                app2 =>
                {
                    if (string.IsNullOrWhiteSpace(env.WebRootPath))
                    {
                        env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    }

                    app2.UseRouting();

                    if (OidcHelper.IsEnabled)
                    {
                        app.UseAuthentication();
                        app.UseAuthorization();
                    }
                    
                    app2.UseEndpoints(e => e.MapFallbackToFile("index.html"));
                    app2.UseFileServer(
                        new FileServerOptions
                        {
                            FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, "v2"))
                        });
                });
        }

        app.UseEndpoints(
            endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");

                // if (env.IsDevelopment())
                // {
                //     endpoints.MapToVueCliProxy(
                //         "/v2/{*path}",
                //         new SpaOptions { SourcePath = "client-app" },
                //         "serve",
                //         regex: "Compiled successfully",
                //         forceKill: true);
                // }
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
        services.AddSingleton<RecyclableMemoryStreamManager>();
        AddChannel<IBackgroundServiceRequest>(services);
        AddChannel<IPlexBackgroundServiceRequest>(services);
        AddChannel<IJellyfinBackgroundServiceRequest>(services);
        AddChannel<IEmbyBackgroundServiceRequest>(services);
        AddChannel<IFFmpegWorkerRequest>(services);
        AddChannel<ISearchIndexBackgroundServiceRequest>(services);
        AddChannel<IScannerBackgroundServiceRequest>(services);

        services.AddScoped<IFFmpegVersionHealthCheck, FFmpegVersionHealthCheck>();
        services.AddScoped<IFFmpegReportsHealthCheck, FFmpegReportsHealthCheck>();
        services.AddScoped<IHardwareAccelerationHealthCheck, HardwareAccelerationHealthCheck>();
        services.AddScoped<IMovieMetadataHealthCheck, MovieMetadataHealthCheck>();
        services.AddScoped<IEpisodeMetadataHealthCheck, EpisodeMetadataHealthCheck>();
        services.AddScoped<IZeroDurationHealthCheck, ZeroDurationHealthCheck>();
        services.AddScoped<IFileNotFoundHealthCheck, FileNotFoundHealthCheck>();
        services.AddScoped<IUnavailableHealthCheck, UnavailableHealthCheck>();
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
        services.AddScoped<ICachingSearchRepository, CachingSearchRepository>();
        services.AddScoped<IMovieRepository, MovieRepository>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IMusicVideoRepository, MusicVideoRepository>();
        services.AddScoped<IOtherVideoRepository, OtherVideoRepository>();
        services.AddScoped<ISongRepository, SongRepository>();
        services.AddScoped<ILibraryRepository, LibraryRepository>();
        services.AddScoped<IMetadataRepository, MetadataRepository>();
        services.AddScoped<IArtworkRepository, ArtworkRepository>();
        services.AddScoped<IFFmpegLocator, FFmpegLocator>();
        services.AddScoped<IFallbackMetadataProvider, FallbackMetadataProvider>();
        services.AddScoped<IPlayoutBuilder, PlayoutBuilder>();
        services.AddScoped<IImageCache, ImageCache>();
        services.AddScoped<ILocalFileSystem, LocalFileSystem>();
        services.AddScoped<IPlexServerApiClient, PlexServerApiClient>();
        services.AddScoped<IPlexMovieRepository, PlexMovieRepository>();
        services.AddScoped<IPlexTelevisionRepository, PlexTelevisionRepository>();
        services.AddScoped<IJellyfinApiClient, JellyfinApiClient>();
        services.AddScoped<IJellyfinPathReplacementService, JellyfinPathReplacementService>();
        services.AddScoped<IJellyfinTelevisionRepository, JellyfinTelevisionRepository>();
        services.AddScoped<IJellyfinCollectionRepository, JellyfinCollectionRepository>();
        services.AddScoped<IJellyfinMovieRepository, JellyfinMovieRepository>();
        services.AddScoped<IEmbyApiClient, EmbyApiClient>();
        services.AddScoped<IEmbyPathReplacementService, EmbyPathReplacementService>();
        services.AddScoped<IEmbyTelevisionRepository, EmbyTelevisionRepository>();
        services.AddScoped<IEmbyCollectionRepository, EmbyCollectionRepository>();
        services.AddScoped<IEmbyMovieRepository, EmbyMovieRepository>();
        services.AddScoped<IRuntimeInfo, RuntimeInfo>();
        services.AddScoped<IPlexPathReplacementService, PlexPathReplacementService>();
        services.AddScoped<IFFmpegStreamSelector, FFmpegStreamSelector>();
        services.AddScoped<IStreamSelectorRepository, StreamSelectorRepository>();
        services.AddScoped<IHardwareCapabilitiesFactory, HardwareCapabilitiesFactory>();
        services.AddScoped<IMultiEpisodeShuffleCollectionEnumeratorFactory,
                MultiEpisodeShuffleCollectionEnumeratorFactory>();

        services.AddScoped<IFFmpegProcessService, FFmpegLibraryProcessService>();
        services.AddScoped<IPipelineBuilderFactory, PipelineBuilderFactory>();
        services.AddScoped<FFmpegProcessService>();

        services.AddScoped<ISongVideoGenerator, SongVideoGenerator>();
        services.AddScoped<IMusicVideoCreditsGenerator, MusicVideoCreditsGenerator>();
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
        services.AddScoped<IScriptEngine, ScriptEngine>();

        services.AddScoped<PlexEtag>();

        // services.AddTransient(typeof(IRequestHandler<,>), typeof(GetRecentLogEntriesHandler<>));

        // run-once/blocking startup services
        services.AddHostedService<EndpointValidatorService>();
        services.AddHostedService<DatabaseMigratorService>();
        services.AddHostedService<LoadLoggingLevelService>();
        services.AddHostedService<CacheCleanerService>();
        services.AddHostedService<ResourceExtractorService>();
        services.AddHostedService<PlatformSettingsService>();
        services.AddHostedService<RebuildSearchIndexService>();

        // background services
#if !DEBUG_NO_SYNC
        services.AddHostedService<EmbyService>();
        services.AddHostedService<JellyfinService>();
        services.AddHostedService<PlexService>();
        services.AddHostedService<ScannerService>();
#endif
        services.AddHostedService<FFmpegLocatorService>();
        services.AddHostedService<WorkerService>();
        services.AddHostedService<SchedulerService>();
        services.AddHostedService<FFmpegWorkerService>();
        services.AddHostedService<SearchIndexService>();
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
