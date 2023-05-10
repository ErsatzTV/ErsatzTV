using System.Threading.Channels;
using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Plex;

public class CallPlexLibraryScannerHandler : CallLibraryScannerHandler<ISynchronizePlexLibraryById>,
    IRequestHandler<ForceSynchronizePlexLibraryById, Either<BaseError, string>>,
    IRequestHandler<SynchronizePlexLibraryByIdIfNeeded, Either<BaseError, string>>
{
    public CallPlexLibraryScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IMediator mediator,
        IRuntimeInfo runtimeInfo)
        : base(dbContextFactory, configElementRepository, channel, mediator, runtimeInfo)
    {
    }

    Task<Either<BaseError, string>> IRequestHandler<ForceSynchronizePlexLibraryById, Either<BaseError, string>>.Handle(
        ForceSynchronizePlexLibraryById request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    Task<Either<BaseError, string>> IRequestHandler<SynchronizePlexLibraryByIdIfNeeded, Either<BaseError, string>>.
        Handle(
            SynchronizePlexLibraryByIdIfNeeded request,
            CancellationToken cancellationToken) => Handle(request, cancellationToken);

    private async Task<Either<BaseError, string>> Handle(
        ISynchronizePlexLibraryById request,
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
        ISynchronizePlexLibraryById request,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "scan-plex", request.PlexLibraryId.ToString()
        };

        if (request.ForceScan)
        {
            arguments.Add("--force");
        }

        if (request.DeepScan)
        {
            arguments.Add("--deep");
        }

        return await base.PerformScan(scanner, arguments, cancellationToken);
    }

    protected override async Task<DateTimeOffset> GetLastScan(
        TvContext dbContext,
        ISynchronizePlexLibraryById request)
    {
        DateTime minDateTime = await dbContext.PlexLibraries
            .SelectOneAsync(l => l.Id, l => l.Id == request.PlexLibraryId)
            .Match(l => l.LastScan ?? SystemTime.MinValueUtc, () => SystemTime.MaxValueUtc);

        return new DateTimeOffset(minDateTime, TimeSpan.Zero);
    }

    protected override bool ScanIsRequired(
        DateTimeOffset lastScan,
        int libraryRefreshInterval,
        ISynchronizePlexLibraryById request)
    {
        if (lastScan == SystemTime.MaxValueUtc)
        {
            return false;
        }

        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(libraryRefreshInterval);
        return request.ForceScan || libraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now;
    }
}
