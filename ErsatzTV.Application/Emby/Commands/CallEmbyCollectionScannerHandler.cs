using System.Threading.Channels;
using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Emby;

public class CallEmbyCollectionScannerHandler : CallLibraryScannerHandler<SynchronizeEmbyCollections>,
    IRequestHandler<SynchronizeEmbyCollections, Either<BaseError, Unit>>
{
    public CallEmbyCollectionScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IMediator mediator,
        IRuntimeInfo runtimeInfo) : base(dbContextFactory, configElementRepository, channel, mediator, runtimeInfo)
    {
    }

    public async Task<Either<BaseError, Unit>>
        Handle(SynchronizeEmbyCollections request, CancellationToken cancellationToken)
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

    protected override async Task<DateTimeOffset> GetLastScan(TvContext dbContext, SynchronizeEmbyCollections request)
    {
        DateTime minDateTime = await dbContext.EmbyMediaSources
            .SelectOneAsync(l => l.Id, l => l.Id == request.EmbyMediaSourceId)
            .Match(l => l.LastCollectionsScan ?? SystemTime.MinValueUtc, () => SystemTime.MaxValueUtc);

        return new DateTimeOffset(minDateTime, TimeSpan.Zero);
    }

    protected override bool ScanIsRequired(
        DateTimeOffset lastScan,
        int libraryRefreshInterval,
        SynchronizeEmbyCollections request)
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
        SynchronizeEmbyCollections request,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "scan-emby-collections", request.EmbyMediaSourceId.ToString()
        };

        if (request.ForceScan)
        {
            arguments.Add("--force");
        }

        return await base.PerformScan(scanner, arguments, cancellationToken).MapT(_ => Unit.Default);
    }
}
