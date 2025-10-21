using System.Globalization;
using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata;
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
    private readonly IScannerProxyService _scannerProxyService;

    public CallPlexLibraryScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        IScannerProxyService scannerProxyService,
        IRuntimeInfo runtimeInfo)
        : base(dbContextFactory, configElementRepository, runtimeInfo)
    {
        _scannerProxyService = scannerProxyService;
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
        Validation<BaseError, ScanParameters> validation = await Validate(request, cancellationToken);
        return await validation.Match(
            parameters => PerformScan(parameters, request, cancellationToken),
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
        ScanParameters parameters,
        ISynchronizePlexLibraryById request,
        CancellationToken cancellationToken)
    {
        Option<Guid> maybeScanId = _scannerProxyService.StartScan(request.PlexLibraryId);
        foreach (var scanId in maybeScanId)
        {
            try
            {
                var arguments = new List<string>
                {
                    "scan-plex",
                    request.PlexLibraryId.ToString(CultureInfo.InvariantCulture),
                    GetBaseUrl(scanId)
                };

                if (request.ForceScan)
                {
                    arguments.Add("--force");
                }

                if (request.DeepScan)
                {
                    arguments.Add("--deep");
                }

                return await base.PerformScan(parameters, arguments, cancellationToken);
            }
            finally
            {
                _scannerProxyService.EndScan(scanId);
            }
        }

        return BaseError.New($"Library {request.PlexLibraryId} is already scanning");
    }

    protected override async Task<Tuple<string, DateTimeOffset>> GetLastScan(
        TvContext dbContext,
        ISynchronizePlexLibraryById request,
        CancellationToken cancellationToken)
    {
        Option<PlexLibrary> maybeLibrary = await dbContext.PlexLibraries
            .SelectOneAsync(l => l.Id, l => l.Id == request.PlexLibraryId, cancellationToken);

        DateTime minDateTime = maybeLibrary.Match(
            l => l.LastScan ?? SystemTime.MinValueUtc,
            () => SystemTime.MaxValueUtc);

        string libraryName = maybeLibrary.Match(l => l.Name, () => string.Empty);

        return new Tuple<string, DateTimeOffset>(libraryName, new DateTimeOffset(minDateTime, TimeSpan.Zero));
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
