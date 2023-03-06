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

public class CallEmbyLibraryScannerHandler : CallLibraryScannerHandler<ISynchronizeEmbyLibraryById>,
    IRequestHandler<ForceSynchronizeEmbyLibraryById, Either<BaseError, string>>,
    IRequestHandler<SynchronizeEmbyLibraryByIdIfNeeded, Either<BaseError, string>>
{
    public CallEmbyLibraryScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IMediator mediator,
        IRuntimeInfo runtimeInfo)
        : base(dbContextFactory, configElementRepository, channel, mediator, runtimeInfo)
    {
    }

    Task<Either<BaseError, string>> IRequestHandler<ForceSynchronizeEmbyLibraryById, Either<BaseError, string>>.Handle(
        ForceSynchronizeEmbyLibraryById request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    Task<Either<BaseError, string>> IRequestHandler<SynchronizeEmbyLibraryByIdIfNeeded, Either<BaseError, string>>.Handle(
        SynchronizeEmbyLibraryByIdIfNeeded request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    private async Task<Either<BaseError, string>> Handle(
        ISynchronizeEmbyLibraryById request,
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
        ISynchronizeEmbyLibraryById request,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "scan-emby", request.EmbyLibraryId.ToString()
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
        ISynchronizeEmbyLibraryById request)
    {
        DateTime minDateTime = await dbContext.EmbyLibraries
            .SelectOneAsync(l => l.Id, l => l.Id == request.EmbyLibraryId)
            .Match(l => l.LastScan ?? SystemTime.MinValueUtc, () => SystemTime.MaxValueUtc);
        
        return new DateTimeOffset(minDateTime, TimeSpan.Zero);
    }

    protected override bool ScanIsRequired(
        DateTimeOffset lastScan,
        int libraryRefreshInterval,
        ISynchronizeEmbyLibraryById request)
    {
        if (lastScan == SystemTime.MaxValueUtc)
        {
            return false;
        }

        DateTimeOffset nextScan = lastScan + TimeSpan.FromHours(libraryRefreshInterval);
        return request.ForceScan || (libraryRefreshInterval > 0 && nextScan < DateTimeOffset.Now);
    }
}
