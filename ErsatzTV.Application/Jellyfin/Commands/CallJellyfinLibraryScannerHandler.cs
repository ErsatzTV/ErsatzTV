using System.Threading.Channels;
using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Jellyfin;

public class CallJellyfinLibraryScannerHandler : CallLibraryScannerHandler<ISynchronizeJellyfinLibraryById>,
    IRequestHandler<ForceSynchronizeJellyfinLibraryById, Either<BaseError, string>>,
    IRequestHandler<SynchronizeJellyfinLibraryByIdIfNeeded, Either<BaseError, string>>
{
    public CallJellyfinLibraryScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IMediator mediator,
        IRuntimeInfo runtimeInfo)
        : base(dbContextFactory, configElementRepository, channel, mediator, runtimeInfo)
    {
    }

    Task<Either<BaseError, string>> IRequestHandler<ForceSynchronizeJellyfinLibraryById, Either<BaseError, string>>.Handle(
        ForceSynchronizeJellyfinLibraryById request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    Task<Either<BaseError, string>> IRequestHandler<SynchronizeJellyfinLibraryByIdIfNeeded, Either<BaseError, string>>.Handle(
        SynchronizeJellyfinLibraryByIdIfNeeded request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    private async Task<Either<BaseError, string>> Handle(
        ISynchronizeJellyfinLibraryById request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, string> validation = await Validate(request);
        return await validation.Match(
            scanner => PerformScan(scanner, request, cancellationToken),
            error =>
            {
                foreach (ScanIsNotRequired scanIsNotRequired in error.OfType<ScanIsNotRequired>())
                {
                    return Task.FromResult<Either<BaseError, string>>(scanIsNotRequired);
                }

                return Task.FromResult<Either<BaseError, string>>(error.Join());
            });
    }

    private async Task<Either<BaseError, string>> PerformScan(
        string scanner,
        ISynchronizeJellyfinLibraryById request,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "scan-jellyfin", request.JellyfinLibraryId.ToString()
        };

        if (request.ForceScan)
        {
            arguments.Add("--force");
        }

        return await base.PerformScan(scanner, arguments, cancellationToken);
    }

    protected override async Task<DateTimeOffset> GetLastScan(
        TvContext dbContext,
        ISynchronizeJellyfinLibraryById request)
    {
        return await dbContext.JellyfinLibraries
            .SelectOneAsync(l => l.Id, l => l.Id == request.JellyfinLibraryId)
            .Match(l => l.LastScan ?? SystemTime.MinValueUtc, () => SystemTime.MaxValueUtc);
    }

    protected override bool ScanIsRequired(
        DateTimeOffset lastScan,
        int libraryRefreshInterval,
        ISynchronizeJellyfinLibraryById request)
    {
        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(libraryRefreshInterval);
        return request.ForceScan || (libraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now);
    }
}
