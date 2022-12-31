using System.Threading.Channels;
using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.FFmpeg.Runtime;

namespace ErsatzTV.Application.Emby;

public class CallEmbyLibraryScannerHandler : CallLibraryScannerHandler,
    IRequestHandler<ForceSynchronizeEmbyLibraryById, Either<BaseError, string>>,
    IRequestHandler<SynchronizeEmbyLibraryByIdIfNeeded, Either<BaseError, string>>
{
    public CallEmbyLibraryScannerHandler(
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IMediator mediator,
        IRuntimeInfo runtimeInfo)
        : base(channel, mediator, runtimeInfo)
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
        Validation<BaseError, string> validation = Validate();
        return await validation.Match(
            scanner => PerformScan(scanner, request, cancellationToken),
            error => Task.FromResult<Either<BaseError, string>>(error.Join()));
    }

    private async Task<Either<BaseError, string>> PerformScan(
        string scanner,
        ISynchronizeEmbyLibraryById request,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "scan",
            "--emby", request.EmbyLibraryId.ToString()
        };

        if (request.ForceScan)
        {
            arguments.Add("--force");
        }

        return await base.PerformScan(scanner, arguments, cancellationToken);
    }
}
