using System.CommandLine;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Scanner.Application.MediaSources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ErsatzTV.Scanner;

public class Worker : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceScopeFactory serviceScopeFactory, IHostApplicationLifetime appLifetime, ILogger<Worker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _appLifetime = appLifetime;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
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

        var rootCommand = new RootCommand();
        rootCommand.AddOption(localOption);
        rootCommand.AddOption(plexOption);
        rootCommand.AddOption(embyOption);
        rootCommand.AddOption(jellyfinOption);

        rootCommand.SetHandler(Handle, localOption, plexOption, embyOption, jellyfinOption);

        // need to strip program name (head) from command line args
        await rootCommand.InvokeAsync(Environment.GetCommandLineArgs().Skip(1).ToArray());

        _appLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task Handle(int? localLibraryId, int? plexLibraryId, int? embyLibraryId, int? jellyfinLibraryId)
    {
#if !DEBUG_NO_SYNC
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        ILocalFileSystem localFileSystem = scope.ServiceProvider.GetRequiredService<ILocalFileSystem>();
        IConfigElementRepository configElementRepository =
            scope.ServiceProvider.GetRequiredService<IConfigElementRepository>();
        ISearchIndex searchIndex = scope.ServiceProvider.GetRequiredService<ISearchIndex>();
        
        await searchIndex.Initialize(localFileSystem, configElementRepository);

        if (localLibraryId is not null)
        {
            _logger.LogInformation("Scanning local library {Id}", localLibraryId);
            var scanLocalLibrary = new ScanLocalLibrary(localLibraryId.Value, true); // TODO: force scan
            await mediator.Send(scanLocalLibrary, CancellationToken.None); // TODO: cancellation
        }
        else if (plexLibraryId is not null)
        {
            _logger.LogInformation("Scanning plex library {Id}", plexLibraryId);
        }
        else if (embyLibraryId is not null)
        {
            _logger.LogInformation("Scanning emby library {Id}", embyLibraryId);
        }
        else if (jellyfinLibraryId is not null)
        {
            _logger.LogInformation("Scanning jellyfin library {Id}", jellyfinLibraryId);
        }
        else
        {
            Log.Logger.Error("No library ids were specified; nothing to scan.");
        }
#else
        Log.Logger.Information("Library scanning is disabled...");
#endif
    }
}
