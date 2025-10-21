using System.Globalization;
using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Plex;

public class CallPlexNetworkScannerHandler : CallLibraryScannerHandler<SynchronizePlexNetworks>,
    IRequestHandler<SynchronizePlexNetworks, Either<BaseError, Unit>>
{
    public CallPlexNetworkScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        IRuntimeInfo runtimeInfo) : base(dbContextFactory, configElementRepository, runtimeInfo)
    {
    }

    public async Task<Either<BaseError, Unit>>
        Handle(SynchronizePlexNetworks request, CancellationToken cancellationToken)
    {
        Validation<BaseError, ScanParameters> validation = await Validate(request, cancellationToken);
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

    protected override async Task<Tuple<string, DateTimeOffset>> GetLastScan(
        TvContext dbContext,
        SynchronizePlexNetworks request,
        CancellationToken cancellationToken)
    {
        DateTime minDateTime = await dbContext.PlexLibraries
            .Filter(l => l.MediaKind == LibraryMediaKind.Shows)
            .SelectOneAsync(l => l.Id, l => l.Id == request.PlexLibraryId, cancellationToken)
            .Match(l => l.LastNetworksScan ?? SystemTime.MinValueUtc, () => SystemTime.MaxValueUtc);

        return new Tuple<string, DateTimeOffset>(string.Empty, new DateTimeOffset(minDateTime, TimeSpan.Zero));
    }

    protected override bool ScanIsRequired(
        DateTimeOffset lastScan,
        int libraryRefreshInterval,
        SynchronizePlexNetworks request)
    {
        if (lastScan == SystemTime.MaxValueUtc)
        {
            return false;
        }

        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(libraryRefreshInterval);
        return request.ForceScan || libraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now;
    }

    private async Task<Either<BaseError, Unit>> PerformScan(
        ScanParameters parameters,
        SynchronizePlexNetworks request,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "scan-plex-networks", request.PlexLibraryId.ToString(CultureInfo.InvariantCulture)
        };

        if (request.ForceScan)
        {
            arguments.Add("--force");
        }

        return await base.PerformScan(parameters, arguments, cancellationToken).MapT(_ => Unit.Default);
    }
}
