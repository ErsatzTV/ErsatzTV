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
        
        var localOption = new System.CommandLine.Option<int?>(
            name: "--local",
            description: "The local library id to scan");

        var plexOption = new System.CommandLine.Option<int?>(
            name: "--plex",
            description: "The plex library id to scan");

        var embyOption = new System.CommandLine.Option<int?>(
            name: "--emby",
            description: "The emby library id to scan");

        var jellyfinOption = new System.CommandLine.Option<int?>(
            name: "--jellyfin",
            description: "The jellyfin library id to scan");
        
        var scanCommand = new Command("scan", "Scan a library");

        scanCommand.AddOption(forceOption);
        scanCommand.AddOption(deepOption);
        scanCommand.AddOption(localOption);
        scanCommand.AddOption(plexOption);
        scanCommand.AddOption(embyOption);
        scanCommand.AddOption(jellyfinOption);

        scanCommand.SetHandler(
            async context =>
            {
                bool force = context.ParseResult.GetValueForOption(forceOption);
                bool deep = context.ParseResult.GetValueForOption(deepOption);
                int? local = context.ParseResult.GetValueForOption(localOption);
                int? plex = context.ParseResult.GetValueForOption(plexOption);
                int? emby = context.ParseResult.GetValueForOption(embyOption);
                int? jellyfin = context.ParseResult.GetValueForOption(jellyfinOption);
                CancellationToken token = context.GetCancellationToken();
                await HandleScan(force, deep, local, plex, emby, jellyfin, token);
            });

        var rootCommand = new RootCommand();
        rootCommand.AddCommand(scanCommand);

        return rootCommand;
    }

    private async Task HandleScan(
        bool forceOption,
        bool deepOption,
        int? localLibraryId,
        int? plexLibraryId,
        int? embyLibraryId,
        int? jellyfinLibraryId,
        CancellationToken cancellationToken)
    {
#if !DEBUG_NO_SYNC
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        if (localLibraryId is not null)
        {
            var scanLocalLibrary = new ScanLocalLibrary(localLibraryId.Value, forceOption);
            await mediator.Send(scanLocalLibrary, cancellationToken);
        }
        else if (plexLibraryId is not null)
        {
            var scanPlexLibrary = new SynchronizePlexLibraryById(plexLibraryId.Value, forceOption, deepOption);
            await mediator.Send(scanPlexLibrary, cancellationToken);
        }
        else if (embyLibraryId is not null)
        {
            var scanEmbyLibrary = new SynchronizeEmbyLibraryById(embyLibraryId.Value, forceOption);
            await mediator.Send(scanEmbyLibrary, cancellationToken);
        }
        else if (jellyfinLibraryId is not null)
        {
            var scanJellyfinLibrary = new SynchronizeJellyfinLibraryById(jellyfinLibraryId.Value, forceOption);
            await mediator.Send(scanJellyfinLibrary, cancellationToken);
        }
        else
        {
            _logger.LogError("No library ids were specified; nothing to scan.");
        }
#else
        _logger.LogInformation("Library scanning is disabled via DEBUG_NO_SYNC...");
#endif
    }
}
