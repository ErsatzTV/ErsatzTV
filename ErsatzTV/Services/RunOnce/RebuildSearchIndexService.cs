using ErsatzTV.Application.Search;
using MediatR;

namespace ErsatzTV.Services.RunOnce;

public class RebuildSearchIndexService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RebuildSearchIndexService(IServiceScopeFactory serviceScopeFactory) =>
        _serviceScopeFactory = serviceScopeFactory;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new RebuildSearchIndex(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
