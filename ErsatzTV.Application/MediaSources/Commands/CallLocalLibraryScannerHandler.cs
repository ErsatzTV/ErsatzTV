using System.Threading.Channels;
using ErsatzTV.Application.Libraries;
using ErsatzTV.Core;
using ErsatzTV.FFmpeg.Runtime;

namespace ErsatzTV.Application.MediaSources;

public class CallLocalLibraryScannerHandler : CallLibraryScannerHandler,
    IRequestHandler<ForceScanLocalLibrary, Either<BaseError, string>>,
    IRequestHandler<ScanLocalLibraryIfNeeded, Either<BaseError, string>>
{
    public CallLocalLibraryScannerHandler(
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IMediator mediator,
        IRuntimeInfo runtimeInfo)
        : base(channel, mediator, runtimeInfo)
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
        Validation<BaseError, string> validation = Validate();
        return await validation.Match(
            scanner => PerformScan(scanner, request, cancellationToken),
            error => Task.FromResult<Either<BaseError, string>>(error.Join()));
    }

    private async Task<Either<BaseError, string>> PerformScan(
        string scanner,
        IScanLocalLibrary request,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "--local",
            request.LibraryId.ToString()
        };

        if (request.ForceScan)
        {
            arguments.Add("--force");
        }

        return await base.PerformScan(scanner, arguments, cancellationToken);
    }
}
