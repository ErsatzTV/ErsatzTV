using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading.Channels;

namespace ErsatzTV.Application.Jellyfin;

public class CallJellyfinShowScannerHandler : CallLibraryScannerHandler<ISynchronizeJellyfinShowByTitle>,
    IRequestHandler<SynchronizeJellyfinShowByTitle, Either<BaseError, string>>
{
    public CallJellyfinShowScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IMediator mediator,
        IRuntimeInfo runtimeInfo)
        : base(dbContextFactory, configElementRepository, channel, mediator, runtimeInfo)
    {
    }

    Task<Either<BaseError, string>> IRequestHandler<SynchronizeJellyfinShowByTitle, Either<BaseError, string>>.Handle(
        SynchronizeJellyfinShowByTitle request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    private async Task<Either<BaseError, string>> Handle(
        ISynchronizeJellyfinShowByTitle request,
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
        ISynchronizeJellyfinShowByTitle request,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "scan-jellyfin-show",
            request.JellyfinLibraryId.ToString(CultureInfo.InvariantCulture),
            request.ShowTitle
        };

        if (request.DeepScan)
        {
            arguments.Add("--deep");
        }

        return await base.PerformScan(scanner, arguments, cancellationToken);
    }

    protected override Task<DateTimeOffset> GetLastScan(
        TvContext dbContext,
        ISynchronizeJellyfinShowByTitle request)
    {
        return Task.FromResult(DateTimeOffset.MinValue);
    }

    protected override bool ScanIsRequired(
        DateTimeOffset lastScan,
        int libraryRefreshInterval,
        ISynchronizeJellyfinShowByTitle request)
    {
        return true;
    }
}