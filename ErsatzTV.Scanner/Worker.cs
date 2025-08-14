using System.CommandLine;
using System.Diagnostics;
using ErsatzTV.Scanner.Application.Emby;
using ErsatzTV.Scanner.Application.Jellyfin;
using ErsatzTV.Scanner.Application.MediaSources;
using ErsatzTV.Scanner.Application.Plex;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner;

public class Worker : BackgroundService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public Worker(
        IServiceScopeFactory serviceScopeFactory,
        IHostApplicationLifetime appLifetime,
        ILogger<Worker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _appLifetime = appLifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RootCommand rootCommand = ConfigureCommandLine();

        // need to strip program name (head) from command line args
        string[] arguments = Environment.GetCommandLineArgs().Skip(1).ToArray();

        ParseResult parseResult = rootCommand.Parse(arguments);
        await parseResult.InvokeAsync(stoppingToken);

        _appLifetime.StopApplication();
    }

    private RootCommand ConfigureCommandLine()
    {
        var forceOption = new System.CommandLine.Option<bool>("--force")
        {
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.Zero,
            Description = "Force scanning",
            DefaultValueFactory = _ => false
        };

        var deepOption = new System.CommandLine.Option<bool>("--deep")
        {
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.Zero,
            Description = "Deep scan",
            DefaultValueFactory = _ => false
        };

        var libraryIdArgument = new Argument<int>("library-id")
        {
            Description = "The library id to scan"
        };
        var mediaSourceIdArgument = new Argument<int>("media-source-id")
        {
            Description = "The media source id to scan"
        };

        var scanLocalCommand = new Command("scan-local", "Scan a local library");
        scanLocalCommand.Arguments.Add(libraryIdArgument);
        scanLocalCommand.Options.Add(forceOption);

        var scanPlexCommand = new Command("scan-plex", "Scan a Plex library");
        scanPlexCommand.Arguments.Add(libraryIdArgument);
        scanPlexCommand.Options.Add(forceOption);
        scanPlexCommand.Options.Add(deepOption);

        var scanPlexCollectionsCommand = new Command("scan-plex-collections", "Scan Plex collections");
        scanPlexCollectionsCommand.Arguments.Add(mediaSourceIdArgument);
        scanPlexCollectionsCommand.Options.Add(forceOption);

        var scanPlexNetworksCommand = new Command("scan-plex-networks", "Scan Plex networks");
        scanPlexNetworksCommand.Arguments.Add(libraryIdArgument);
        scanPlexNetworksCommand.Options.Add(forceOption);

        var scanEmbyCommand = new Command("scan-emby", "Scan an Emby library");
        scanEmbyCommand.Arguments.Add(libraryIdArgument);
        scanEmbyCommand.Options.Add(forceOption);
        scanEmbyCommand.Options.Add(deepOption);

        var scanEmbyCollectionsCommand = new Command("scan-emby-collections", "Scan Emby collections");
        scanEmbyCollectionsCommand.Arguments.Add(mediaSourceIdArgument);
        scanEmbyCollectionsCommand.Options.Add(forceOption);

        var scanJellyfinCommand = new Command("scan-jellyfin", "Scan a Jellyfin library");
        scanJellyfinCommand.Arguments.Add(libraryIdArgument);
        scanJellyfinCommand.Options.Add(forceOption);
        scanJellyfinCommand.Options.Add(deepOption);

        var scanJellyfinCollectionsCommand = new Command("scan-jellyfin-collections", "Scan Jellyfin collections");
        scanJellyfinCollectionsCommand.Arguments.Add(mediaSourceIdArgument);
        scanJellyfinCollectionsCommand.Options.Add(forceOption);

        // Show-specific scanning commands
        var showTitleArgument = new Argument<string>("show-title")
        {
            Description = "The title of the TV show to scan"
        };

        var showIdArgument = new Argument<int>("show-id")
        {
            Description = "The id of the TV show to scan"
        };

        var scanPlexShowCommand = new Command("scan-plex-show", "Scan a specific TV show in a Plex library");
        scanPlexShowCommand.Arguments.Add(libraryIdArgument);
        scanPlexShowCommand.Arguments.Add(showIdArgument);
        scanPlexShowCommand.Options.Add(deepOption);

        var scanEmbyShowCommand = new Command("scan-emby-show", "Scan a specific TV show in an Emby library");
        scanEmbyShowCommand.Arguments.Add(libraryIdArgument);
        scanEmbyShowCommand.Arguments.Add(showTitleArgument);
        scanEmbyShowCommand.Options.Add(deepOption);

        var scanJellyfinShowCommand = new Command("scan-jellyfin-show", "Scan a specific TV show in a Jellyfin library");
        scanJellyfinShowCommand.Arguments.Add(libraryIdArgument);
        scanJellyfinShowCommand.Arguments.Add(showTitleArgument);
        scanJellyfinShowCommand.Options.Add(deepOption);

        scanLocalCommand.SetAction(async (parseResult, token) =>
        {
            if (IsScanningEnabled())
            {
                bool force = parseResult.GetValue(forceOption);
                SetProcessPriority(force);

                int libraryId = parseResult.GetValue(libraryIdArgument);

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var scan = new ScanLocalLibrary(libraryId, force);
                await mediator.Send(scan, token);
            }
        });

        scanPlexCommand.SetAction(async (parseResult, token) =>
        {
            if (IsScanningEnabled())
            {
                bool force = parseResult.GetValue(forceOption);
                SetProcessPriority(force);

                bool deep = parseResult.GetValue(deepOption);
                int libraryId = parseResult.GetValue(libraryIdArgument);

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var scan = new SynchronizePlexLibraryById(libraryId, force, deep);
                await mediator.Send(scan, token);
            }
        });

        scanPlexCollectionsCommand.SetAction(async (parseResult, token) =>
        {
            if (IsScanningEnabled())
            {
                bool force = parseResult.GetValue(forceOption);
                SetProcessPriority(force);

                int mediaSourceId = parseResult.GetValue(mediaSourceIdArgument);

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var scan = new SynchronizePlexCollections(mediaSourceId, force);
                await mediator.Send(scan, token);
            }
        });

        scanPlexNetworksCommand.SetAction(async (parseResult, token) =>
        {
            if (IsScanningEnabled())
            {
                bool force = parseResult.GetValue(forceOption);
                SetProcessPriority(force);

                int libraryId = parseResult.GetValue(libraryIdArgument);

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var scan = new SynchronizePlexNetworks(libraryId, force);
                await mediator.Send(scan, token);
            }
        });

        scanEmbyCommand.SetAction(async (parseResult, token) =>
        {
            if (IsScanningEnabled())
            {
                bool force = parseResult.GetValue(forceOption);
                SetProcessPriority(force);

                bool deep = parseResult.GetValue(deepOption);
                int libraryId = parseResult.GetValue(libraryIdArgument);

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var scan = new SynchronizeEmbyLibraryById(libraryId, force, deep);
                await mediator.Send(scan, token);
            }
        });

        scanEmbyCollectionsCommand.SetAction(async (parseResult, token) =>
        {
            if (IsScanningEnabled())
            {
                bool force = parseResult.GetValue(forceOption);
                SetProcessPriority(force);

                int mediaSourceId = parseResult.GetValue(mediaSourceIdArgument);

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var scan = new SynchronizeEmbyCollections(mediaSourceId, force);
                await mediator.Send(scan, token);
            }
        });

        scanJellyfinCommand.SetAction(async (parseResult, token) =>
        {
            if (IsScanningEnabled())
            {
                bool force = parseResult.GetValue(forceOption);
                SetProcessPriority(force);

                bool deep = parseResult.GetValue(deepOption);
                int libraryId = parseResult.GetValue(libraryIdArgument);

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var scan = new SynchronizeJellyfinLibraryById(libraryId, force, deep);
                await mediator.Send(scan, token);
            }
        });

        scanJellyfinCollectionsCommand.SetAction(async (parseResult, token) =>
        {
            if (IsScanningEnabled())
            {
                bool force = parseResult.GetValue(forceOption);
                SetProcessPriority(force);

                int mediaSourceId = parseResult.GetValue(mediaSourceIdArgument);

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var scan = new SynchronizeJellyfinCollections(mediaSourceId, force);
                await mediator.Send(scan, token);
            }
        });

        scanPlexShowCommand.SetAction(async (parseResult, token) =>
        {
            if (IsScanningEnabled())
            {
                bool deep = parseResult.GetValue(deepOption);
                int libraryId = parseResult.GetValue(libraryIdArgument);
                int showId = parseResult.GetValue(showIdArgument);

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var scan = new SynchronizePlexShowById(libraryId, showId, deep);
                await mediator.Send(scan, token);
            }
        });

        scanEmbyShowCommand.SetAction(async (parseResult, token) =>
        {
            if (IsScanningEnabled())
            {
                bool deep = parseResult.GetValue(deepOption);
                int libraryId = parseResult.GetValue(libraryIdArgument);
                string? showTitle = parseResult.GetValue(showTitleArgument);
                if (string.IsNullOrWhiteSpace(showTitle))
                {
                    return;
                }

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var scan = new SynchronizeEmbyShowByTitle(libraryId, showTitle, deep);
                await mediator.Send(scan, token);
            }
        });

        scanJellyfinShowCommand.SetAction(async (parseResult, token) =>
        {
            if (IsScanningEnabled())
            {
                bool deep = parseResult.GetValue(deepOption);
                int libraryId = parseResult.GetValue(libraryIdArgument);
                string? showTitle = parseResult.GetValue(showTitleArgument);
                if (string.IsNullOrWhiteSpace(showTitle))
                {
                    return;
                }

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var scan = new SynchronizeJellyfinShowByTitle(libraryId, showTitle, deep);
                await mediator.Send(scan, token);
            }
        });

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(scanLocalCommand);
        rootCommand.Subcommands.Add(scanPlexCommand);
        rootCommand.Subcommands.Add(scanPlexCollectionsCommand);
        rootCommand.Subcommands.Add(scanPlexNetworksCommand);
        rootCommand.Subcommands.Add(scanEmbyCommand);
        rootCommand.Subcommands.Add(scanEmbyCollectionsCommand);
        rootCommand.Subcommands.Add(scanJellyfinCommand);
        rootCommand.Subcommands.Add(scanJellyfinCollectionsCommand);
        rootCommand.Subcommands.Add(scanPlexShowCommand);
        rootCommand.Subcommands.Add(scanEmbyShowCommand);
        rootCommand.Subcommands.Add(scanJellyfinShowCommand);

        return rootCommand;
    }

    private bool IsScanningEnabled()
    {
#if !DEBUG_NO_SYNC
        // don't want to flag the logger as unused (only used when sync is disabled)
        ILogger<Worker> _ = _logger;
        return true;
#else
        _logger.LogInformation("Scanning is disabled via DEBUG_NO_SYNC");
        return false;
#endif
    }

    private void SetProcessPriority(bool force)
    {
        if (force)
        {
            return;
        }

        try
        {
            using var process = Process.GetCurrentProcess();
            process.PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set scanner priority");
        }
    }
}
