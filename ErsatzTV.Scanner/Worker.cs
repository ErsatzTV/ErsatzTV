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

        await rootCommand.InvokeAsync(arguments);

        _appLifetime.StopApplication();
    }

    private RootCommand ConfigureCommandLine()
    {
        var forceOption = new System.CommandLine.Option<bool>(
            "--force",
            description: "Force scanning",
            parseArgument: _ => true)
        {
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.Zero
        };

        var deepOption = new System.CommandLine.Option<bool>(
            "--deep",
            description: "Deep scan",
            parseArgument: _ => true)
        {
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.Zero
        };

        var libraryIdArgument = new Argument<int>("library-id", "The library id to scan");
        var mediaSourceIdArgument = new Argument<int>("media-source-id", "The media source id to scan");

        var scanLocalCommand = new Command("scan-local", "Scan a local library");
        scanLocalCommand.AddArgument(libraryIdArgument);
        scanLocalCommand.AddOption(forceOption);

        var scanPlexCommand = new Command("scan-plex", "Scan a Plex library");
        scanPlexCommand.AddArgument(libraryIdArgument);
        scanPlexCommand.AddOption(forceOption);
        scanPlexCommand.AddOption(deepOption);

        var scanEmbyCommand = new Command("scan-emby", "Scan an Emby library");
        scanEmbyCommand.AddArgument(libraryIdArgument);
        scanEmbyCommand.AddOption(forceOption);
        scanEmbyCommand.AddOption(deepOption);

        var scanEmbyCollectionsCommand = new Command("scan-emby-collections", "Scan Emby collections");
        scanEmbyCollectionsCommand.AddArgument(mediaSourceIdArgument);
        scanEmbyCollectionsCommand.AddOption(forceOption);

        var scanJellyfinCommand = new Command("scan-jellyfin", "Scan a Jellyfin library");
        scanJellyfinCommand.AddArgument(libraryIdArgument);
        scanJellyfinCommand.AddOption(forceOption);
        scanJellyfinCommand.AddOption(deepOption);

        var scanJellyfinCollectionsCommand = new Command("scan-jellyfin-collections", "Scan Jellyfin collections");
        scanJellyfinCollectionsCommand.AddArgument(mediaSourceIdArgument);
        scanJellyfinCollectionsCommand.AddOption(forceOption);

        scanLocalCommand.SetHandler(
            async context =>
            {
                if (IsScanningEnabled())
                {
                    bool force = context.ParseResult.GetValueForOption(forceOption);
                    SetProcessPriority(force);

                    int libraryId = context.ParseResult.GetValueForArgument(libraryIdArgument);

                    using IServiceScope scope = _serviceScopeFactory.CreateScope();
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var scan = new ScanLocalLibrary(libraryId, force);
                    await mediator.Send(scan, context.GetCancellationToken());
                }
            });

        scanPlexCommand.SetHandler(
            async context =>
            {
                if (IsScanningEnabled())
                {
                    bool force = context.ParseResult.GetValueForOption(forceOption);
                    SetProcessPriority(force);

                    bool deep = context.ParseResult.GetValueForOption(deepOption);
                    int libraryId = context.ParseResult.GetValueForArgument(libraryIdArgument);

                    using IServiceScope scope = _serviceScopeFactory.CreateScope();
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var scan = new SynchronizePlexLibraryById(libraryId, force, deep);
                    await mediator.Send(scan, context.GetCancellationToken());
                }
            });

        scanEmbyCommand.SetHandler(
            async context =>
            {
                if (IsScanningEnabled())
                {
                    bool force = context.ParseResult.GetValueForOption(forceOption);
                    SetProcessPriority(force);

                    bool deep = context.ParseResult.GetValueForOption(deepOption);
                    int libraryId = context.ParseResult.GetValueForArgument(libraryIdArgument);

                    using IServiceScope scope = _serviceScopeFactory.CreateScope();
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var scan = new SynchronizeEmbyLibraryById(libraryId, force, deep);
                    await mediator.Send(scan, context.GetCancellationToken());
                }
            });

        scanEmbyCollectionsCommand.SetHandler(
            async context =>
            {
                if (IsScanningEnabled())
                {
                    bool force = context.ParseResult.GetValueForOption(forceOption);
                    SetProcessPriority(force);

                    int mediaSourceId = context.ParseResult.GetValueForArgument(mediaSourceIdArgument);

                    using IServiceScope scope = _serviceScopeFactory.CreateScope();
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var scan = new SynchronizeEmbyCollections(mediaSourceId, force);
                    await mediator.Send(scan, context.GetCancellationToken());
                }
            });

        scanJellyfinCommand.SetHandler(
            async context =>
            {
                if (IsScanningEnabled())
                {
                    bool force = context.ParseResult.GetValueForOption(forceOption);
                    SetProcessPriority(force);

                    bool deep = context.ParseResult.GetValueForOption(deepOption);
                    int libraryId = context.ParseResult.GetValueForArgument(libraryIdArgument);

                    using IServiceScope scope = _serviceScopeFactory.CreateScope();
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var scan = new SynchronizeJellyfinLibraryById(libraryId, force, deep);
                    await mediator.Send(scan, context.GetCancellationToken());
                }
            });
        
        scanJellyfinCollectionsCommand.SetHandler(
            async context =>
            {
                if (IsScanningEnabled())
                {
                    bool force = context.ParseResult.GetValueForOption(forceOption);
                    SetProcessPriority(force);

                    int mediaSourceId = context.ParseResult.GetValueForArgument(mediaSourceIdArgument);

                    using IServiceScope scope = _serviceScopeFactory.CreateScope();
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var scan = new SynchronizeJellyfinCollections(mediaSourceId, force);
                    await mediator.Send(scan, context.GetCancellationToken());
                }
            });

        var rootCommand = new RootCommand();
        rootCommand.AddCommand(scanLocalCommand);
        rootCommand.AddCommand(scanPlexCommand);
        rootCommand.AddCommand(scanEmbyCommand);
        rootCommand.AddCommand(scanEmbyCollectionsCommand);
        rootCommand.AddCommand(scanJellyfinCommand);
        rootCommand.AddCommand(scanJellyfinCollectionsCommand);

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
