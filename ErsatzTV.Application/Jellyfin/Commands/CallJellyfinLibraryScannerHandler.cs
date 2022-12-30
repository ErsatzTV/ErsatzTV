using System.Threading.Channels;
using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.FFmpeg.Runtime;

namespace ErsatzTV.Application.Jellyfin;

public class CallJellyfinLibraryScannerHandler : CallLibraryScannerHandler,
    IRequestHandler<ForceSynchronizeJellyfinLibraryById, Either<BaseError, string>>,
    IRequestHandler<SynchronizeJellyfinLibraryByIdIfNeeded, Either<BaseError, string>>
{
    public CallJellyfinLibraryScannerHandler(
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IMediator mediator,
        IRuntimeInfo runtimeInfo)
        : base(channel, mediator, runtimeInfo)
    {
    }

    Task<Either<BaseError, string>> IRequestHandler<ForceSynchronizeJellyfinLibraryById, Either<BaseError, string>>.Handle(
        ForceSynchronizeJellyfinLibraryById request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    Task<Either<BaseError, string>> IRequestHandler<SynchronizeJellyfinLibraryByIdIfNeeded, Either<BaseError, string>>.Handle(
        SynchronizeJellyfinLibraryByIdIfNeeded request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    private async Task<Either<BaseError, string>> Handle(
        ISynchronizeJellyfinLibraryById request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, string> validation = Validate();
        return await validation.Match(
            scanner => PerformScan(scanner, request, cancellationToken),
            error => Task.FromResult<Either<BaseError, string>>(error.Join()));
    }

    private async Task<Either<BaseError, string>> PerformScan(
        string scanner,
        ISynchronizeJellyfinLibraryById request,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "--jellyfin",
            request.JellyfinLibraryId.ToString()
        };

        if (request.ForceScan)
        {
            arguments.Add("--force");
        }

        return await base.PerformScan(scanner, arguments, cancellationToken);
    }
}
