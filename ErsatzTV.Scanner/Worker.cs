using System.CommandLine;
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
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<Worker> _logger;

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
            name: "--force",
            description: "Force scanning",
            parseArgument: _ => true)
        {
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.Zero
        };

        var deepOption = new System.CommandLine.Option<bool>(
            name: "--deep",
            description: "Deep scan",
            parseArgument: _ => true)
        {
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.Zero
        };
        
        var libraryIdArgument = new Argument<int>("library-id", "The library id to scan");
        
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

        var scanJellyfinCommand = new Command("scan-jellyfin", "Scan a Jellyfin library");
        scanJellyfinCommand.AddArgument(libraryIdArgument);
        scanJellyfinCommand.AddOption(forceOption);
        
        scanLocalCommand.SetHandler(
            async context =>
            {
                if (IsScanningEnabled())
                {
                    bool force = context.ParseResult.GetValueForOption(forceOption);
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
                    int libraryId = context.ParseResult.GetValueForArgument(libraryIdArgument);

                    using IServiceScope scope = _serviceScopeFactory.CreateScope();
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var scan = new SynchronizeEmbyLibraryById(libraryId, force);
                    await mediator.Send(scan, context.GetCancellationToken());
                }
            });
        
        scanJellyfinCommand.SetHandler(
            async context =>
            {
                if (IsScanningEnabled())
                {
                    bool force = context.ParseResult.GetValueForOption(forceOption);
                    int libraryId = context.ParseResult.GetValueForArgument(libraryIdArgument);

                    using IServiceScope scope = _serviceScopeFactory.CreateScope();
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var scan = new SynchronizeJellyfinLibraryById(libraryId, force);
                    await mediator.Send(scan, context.GetCancellationToken());
                }
            });

        var rootCommand = new RootCommand();
        rootCommand.AddCommand(scanLocalCommand);
        rootCommand.AddCommand(scanPlexCommand);
        rootCommand.AddCommand(scanEmbyCommand);
        rootCommand.AddCommand(scanJellyfinCommand);

        return rootCommand;
    }

#if !DEBUG_NO_SYNC
    private bool IsScanningEnabled()
    {
        // don't want to flag the logger as unused (only used when sync is disabled)
        ILogger<Worker> _ = _logger;
        return true;
    }
#else
    private bool IsScanningEnabled()
    {
        _logger.LogInformation("Scanning is disabled via DEBUG_NO_SYNC");
        return false;
    }
#endif
}
