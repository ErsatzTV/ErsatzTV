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

namespace ErsatzTV.Application.Jellyfin;

public class CallJellyfinLibraryScannerHandler : CallLibraryScannerHandler<ISynchronizeJellyfinLibraryById>,
    IRequestHandler<ForceSynchronizeJellyfinLibraryById, Either<BaseError, string>>,
    IRequestHandler<SynchronizeJellyfinLibraryByIdIfNeeded, Either<BaseError, string>>
{
    private readonly IScannerProxyService _scannerProxyService;

    public CallJellyfinLibraryScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        IScannerProxyService scannerProxyService,
        IRuntimeInfo runtimeInfo)
        : base(dbContextFactory, configElementRepository, runtimeInfo)
    {
        _scannerProxyService = scannerProxyService;
    }

    Task<Either<BaseError, string>> IRequestHandler<ForceSynchronizeJellyfinLibraryById, Either<BaseError, string>>.
        Handle(
            ForceSynchronizeJellyfinLibraryById request,
            CancellationToken cancellationToken) => Handle(request, cancellationToken);

    Task<Either<BaseError, string>> IRequestHandler<SynchronizeJellyfinLibraryByIdIfNeeded, Either<BaseError, string>>.
        Handle(
            SynchronizeJellyfinLibraryByIdIfNeeded request,
            CancellationToken cancellationToken) => Handle(request, cancellationToken);

    private async Task<Either<BaseError, string>> Handle(
        ISynchronizeJellyfinLibraryById request,
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
        ISynchronizeJellyfinLibraryById request,
        CancellationToken cancellationToken)
    {
        Option<Guid> maybeScanId = _scannerProxyService.StartScan(request.JellyfinLibraryId);
        foreach (var scanId in maybeScanId)
        {
            try
            {
                var arguments = new List<string>
                {
                    "scan-jellyfin",
                    request.JellyfinLibraryId.ToString(CultureInfo.InvariantCulture),
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

        return BaseError.New($"Library {request.JellyfinLibraryId} is already scanning");
    }

    protected override async Task<Tuple<string, DateTimeOffset>> GetLastScan(
        TvContext dbContext,
        ISynchronizeJellyfinLibraryById request,
        CancellationToken cancellationToken)
    {
        Option<JellyfinLibrary> maybeLibrary = await dbContext.JellyfinLibraries
            .SelectOneAsync(l => l.Id, l => l.Id == request.JellyfinLibraryId, cancellationToken);

        DateTime minDateTime = maybeLibrary.Match(
            l => l.LastScan ?? SystemTime.MinValueUtc,
            () => SystemTime.MaxValueUtc);

        string libraryName = maybeLibrary.Match(l => l.Name, string.Empty);

        return new Tuple<string, DateTimeOffset>(libraryName, new DateTimeOffset(minDateTime, TimeSpan.Zero));
    }

    protected override bool ScanIsRequired(
        DateTimeOffset lastScan,
        int libraryRefreshInterval,
        ISynchronizeJellyfinLibraryById request)
    {
        if (lastScan == SystemTime.MaxValueUtc)
        {
            return false;
        }

        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(libraryRefreshInterval);
        return request.ForceScan || libraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now;
    }
}
