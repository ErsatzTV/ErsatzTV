using System.Globalization;
using System.Threading.Channels;
using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaSources;

public class CallLocalLibraryScannerHandler : CallLibraryScannerHandler<IScanLocalLibrary>,
    IRequestHandler<ForceScanLocalLibrary, Either<BaseError, string>>,
    IRequestHandler<ScanLocalLibraryIfNeeded, Either<BaseError, string>>
{
    public CallLocalLibraryScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IMediator mediator,
        IRuntimeInfo runtimeInfo)
        : base(dbContextFactory, configElementRepository, channel, mediator, runtimeInfo)
    {
    }

    Task<Either<BaseError, string>> IRequestHandler<ForceScanLocalLibrary, Either<BaseError, string>>.Handle(
        ForceScanLocalLibrary request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    Task<Either<BaseError, string>> IRequestHandler<ScanLocalLibraryIfNeeded, Either<BaseError, string>>.Handle(
        ScanLocalLibraryIfNeeded request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    private async Task<Either<BaseError, string>> Handle(IScanLocalLibrary request, CancellationToken cancellationToken)
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
        IScanLocalLibrary request,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "scan-local", request.LibraryId.ToString(CultureInfo.InvariantCulture)
        };

        if (request.ForceScan)
        {
            arguments.Add("--force");
        }

        return await base.PerformScan(scanner, arguments, cancellationToken);
    }

    protected override async Task<DateTimeOffset> GetLastScan(TvContext dbContext, IScanLocalLibrary request)
    {
        List<LibraryPath> libraryPaths = await dbContext.LibraryPaths
            .Filter(lp => lp.LibraryId == request.LibraryId)
            .ToListAsync();

        DateTime minDateTime = libraryPaths.Any()
            ? libraryPaths.Min(lp => lp.LastScan ?? SystemTime.MinValueUtc)
            : SystemTime.MaxValueUtc;

        return new DateTimeOffset(minDateTime, TimeSpan.Zero);
    }

    protected override bool ScanIsRequired(
        DateTimeOffset lastScan,
        int libraryRefreshInterval,
        IScanLocalLibrary request)
    {
        if (lastScan == SystemTime.MaxValueUtc)
        {
            return false;
        }

        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(libraryRefreshInterval);
        return request.ForceScan || libraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now;
    }
}
