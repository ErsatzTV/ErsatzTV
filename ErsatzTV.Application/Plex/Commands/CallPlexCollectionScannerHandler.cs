using System.Globalization;
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

public class CallPlexCollectionScannerHandler : CallLibraryScannerHandler<SynchronizePlexCollections>,
    IRequestHandler<SynchronizePlexCollections, Either<BaseError, Unit>>
{
    public CallPlexCollectionScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IMediator mediator,
        IRuntimeInfo runtimeInfo) : base(dbContextFactory, configElementRepository, channel, mediator, runtimeInfo)
    {
    }

    public async Task<Either<BaseError, Unit>>
        Handle(SynchronizePlexCollections request, CancellationToken cancellationToken)
    {
        Validation<BaseError, string> validation = await Validate(request);
        return await validation.Match(
            scanner => PerformScan(scanner, request, cancellationToken),
            error =>
            {
                foreach (ScanIsNotRequired scanIsNotRequired in error.OfType<ScanIsNotRequired>())
                {
                    return Task.FromResult<Either<BaseError, Unit>>(scanIsNotRequired);
                }

                return Task.FromResult<Either<BaseError, Unit>>(error.Join());
            });
    }

    protected override async Task<DateTimeOffset> GetLastScan(TvContext dbContext, SynchronizePlexCollections request)
    {
        DateTime minDateTime = await dbContext.PlexMediaSources
            .SelectOneAsync(l => l.Id, l => l.Id == request.PlexMediaSourceId)
            .Match(l => l.LastCollectionsScan ?? SystemTime.MinValueUtc, () => SystemTime.MaxValueUtc);

        return new DateTimeOffset(minDateTime, TimeSpan.Zero);
    }

    protected override bool ScanIsRequired(
        DateTimeOffset lastScan,
        int libraryRefreshInterval,
        SynchronizePlexCollections request)
    {
        if (lastScan == SystemTime.MaxValueUtc)
        {
            return false;
        }

        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(libraryRefreshInterval);
        return request.ForceScan || libraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now;
    }

    private async Task<Either<BaseError, Unit>> PerformScan(
        string scanner,
        SynchronizePlexCollections request,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "scan-plex-collections", request.PlexMediaSourceId.ToString(CultureInfo.InvariantCulture)
        };

        if (request.ForceScan)
        {
            arguments.Add("--force");
        }

        return await base.PerformScan(scanner, arguments, cancellationToken).MapT(_ => Unit.Default);
    }
}
