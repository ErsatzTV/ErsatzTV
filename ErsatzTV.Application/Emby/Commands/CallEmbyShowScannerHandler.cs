using System.Globalization;
using System.Threading.Channels;
using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Emby;

public class CallEmbyShowScannerHandler : CallLibraryScannerHandler<SynchronizeEmbyShowById>,
    IRequestHandler<SynchronizeEmbyShowById, Either<BaseError, string>>
{
    private readonly IScannerProxyService _scannerProxyService;

    public CallEmbyShowScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        IScannerProxyService scannerProxyService,
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IRuntimeInfo runtimeInfo)
        : base(dbContextFactory, configElementRepository, channel, runtimeInfo)
    {
        _scannerProxyService = scannerProxyService;
    }

    Task<Either<BaseError, string>> IRequestHandler<SynchronizeEmbyShowById, Either<BaseError, string>>.Handle(
        SynchronizeEmbyShowById request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    private async Task<Either<BaseError, string>> Handle(
        SynchronizeEmbyShowById request,
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
        SynchronizeEmbyShowById request,
        CancellationToken cancellationToken)
    {
        Option<Guid> maybeScanId = _scannerProxyService.StartScan(request.EmbyLibraryId);
        foreach (var scanId in maybeScanId)
        {
            try
            {
                var arguments = new List<string>
                {
                    "scan-emby-show",
                    request.EmbyLibraryId.ToString(CultureInfo.InvariantCulture),
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

        return BaseError.New($"Library {request.EmbyLibraryId} is already scanning");
    }

    protected override Task<Tuple<string, DateTimeOffset>> GetLastScan(
        TvContext dbContext,
        SynchronizeEmbyShowById request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new Tuple<string, DateTimeOffset>(string.Empty, DateTimeOffset.MinValue));

    protected override bool ScanIsRequired(
        DateTimeOffset lastScan,
        int libraryRefreshInterval,
        SynchronizeEmbyShowById request) =>
        true;
}
