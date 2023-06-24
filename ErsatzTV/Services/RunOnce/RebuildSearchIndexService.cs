using ErsatzTV.Application.Search;
using ErsatzTV.Core;
using MediatR;

namespace ErsatzTV.Services.RunOnce;

public class RebuildSearchIndexService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SystemStartup _systemStartup;

    public RebuildSearchIndexService(IServiceScopeFactory serviceScopeFactory, SystemStartup systemStartup)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _systemStartup = systemStartup;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        Serilog.Log.Information("{0} waiting for database", nameof(RebuildSearchIndexService));

        await _systemStartup.WaitForDatabase(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        Serilog.Log.Information("{0} waiting for database", nameof(RebuildSearchIndexService));

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new RebuildSearchIndex(), cancellationToken);
    }
}
