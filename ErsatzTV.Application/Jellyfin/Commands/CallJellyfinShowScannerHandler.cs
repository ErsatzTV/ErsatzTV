using System.Globalization;
using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Jellyfin;

public class CallJellyfinShowScannerHandler : CallLibraryScannerHandler<SynchronizeJellyfinShowById>,
    IRequestHandler<SynchronizeJellyfinShowById, Either<BaseError, string>>
{
    private readonly IScannerProxyService _scannerProxyService;

    public CallJellyfinShowScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        IScannerProxyService scannerProxyService,
        IRuntimeInfo runtimeInfo)
        : base(dbContextFactory, configElementRepository, runtimeInfo)
    {
        _scannerProxyService = scannerProxyService;
    }

    Task<Either<BaseError, string>> IRequestHandler<SynchronizeJellyfinShowById, Either<BaseError, string>>.Handle(
        SynchronizeJellyfinShowById request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    private async Task<Either<BaseError, string>> Handle(
        SynchronizeJellyfinShowById request,
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
        SynchronizeJellyfinShowById request,
        CancellationToken cancellationToken)
    {
        Option<Guid> maybeScanId = _scannerProxyService.StartScan(request.JellyfinLibraryId);
        foreach (var scanId in maybeScanId)
        {
            try
            {
                var arguments = new List<string>
                {
                    "scan-jellyfin-show",
                    request.JellyfinLibraryId.ToString(CultureInfo.InvariantCulture),
                    request.ShowId.ToString(CultureInfo.InvariantCulture),
                    GetBaseUrl(scanId)
                };

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

    protected override Task<Tuple<string, DateTimeOffset>> GetLastScan(
        TvContext dbContext,
        SynchronizeJellyfinShowById request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new Tuple<string, DateTimeOffset>(string.Empty, DateTimeOffset.MinValue));

    protected override bool ScanIsRequired(
        DateTimeOffset lastScan,
        int libraryRefreshInterval,
        SynchronizeJellyfinShowById request) =>
        true;
}
