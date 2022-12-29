using System.Diagnostics;
using System.Runtime.InteropServices;
using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Metadata;
using ErsatzTV.FFmpeg.Runtime;
using Newtonsoft.Json;
using Serilog;
using Serilog.Formatting.Compact.Reader;

namespace ErsatzTV.Application.Plex;

public class CallPlexLibraryScannerHandler : IRequestHandler<ForceSynchronizePlexLibraryById, Either<BaseError, string>>,
    IRequestHandler<SynchronizePlexLibraryByIdIfNeeded, Either<BaseError, string>>
{
    private readonly IMediator _mediator;
    private readonly IRuntimeInfo _runtimeInfo;

    public CallPlexLibraryScannerHandler(IMediator mediator, IRuntimeInfo runtimeInfo)
    {
        _mediator = mediator;
        _runtimeInfo = runtimeInfo;
    }

    Task<Either<BaseError, string>> IRequestHandler<ForceSynchronizePlexLibraryById, Either<BaseError, string>>.Handle(
        ForceSynchronizePlexLibraryById request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    Task<Either<BaseError, string>> IRequestHandler<SynchronizePlexLibraryByIdIfNeeded, Either<BaseError, string>>.Handle(
        SynchronizePlexLibraryByIdIfNeeded request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);

    private async Task<Either<BaseError, string>> Handle(ISynchronizePlexLibraryById request, CancellationToken cancellationToken)
    {
        Validation<BaseError, string> validation = Validate();
        return await validation.Match(
            scanner => PerformScan(scanner, request, cancellationToken),
            error => Task.FromResult<Either<BaseError, string>>(error.Join()));
    }

    private async Task<Either<BaseError, string>> PerformScan(
        string scanner,
        ISynchronizePlexLibraryById request,
        CancellationToken cancellationToken)
    {
        try
        {
            var arguments = new List<string>
            {
                "--plex",
                request.PlexLibraryId.ToString()
            };

            if (request.ForceScan)
            {
                arguments.Add("--force");
            }

            if (request.DeepScan)
            {
                arguments.Add("--deep");
            }

            using var forcefulCts = new CancellationTokenSource();

            await using CancellationTokenRegistration link = cancellationToken.Register(
                () => forcefulCts.CancelAfter(TimeSpan.FromSeconds(10))
            );

            CommandResult process = await Cli.Wrap(scanner)
                .WithArguments(arguments)
                .WithValidation(CommandResultValidation.None)
                .WithStandardErrorPipe(PipeTarget.ToDelegate(ProcessLogOutput))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(ProcessProgressOutput))
                .ExecuteAsync(forcefulCts.Token, cancellationToken);

            if (process.ExitCode != 0)
            {
                return BaseError.New($"ErsatzTV.Scanner exited with code {process.ExitCode}");
            }

            // TODO: return plex library name?
            return string.Empty;
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
    }

    private static void ProcessLogOutput(string s)
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            try
            {
                Log.Write(LogEventReader.ReadFromString(s));
            }
            catch
            {
                Console.WriteLine(s);
            }
        }
    }

    private async Task ProcessProgressOutput(string s)
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            try
            {
                LibraryScanProgress libraryScanProgress = JsonConvert.DeserializeObject<LibraryScanProgress>(s);
                await _mediator.Publish(libraryScanProgress);
            }
            catch
            {
                // do nothing
            }
        }
    }

    private Validation<BaseError, string> Validate()
    {
        string executable = _runtimeInfo.IsOSPlatform(OSPlatform.Windows)
            ? "ErsatzTV.Scanner.exe"
            : "ErsatzTV.Scanner";
        
        string processFileName = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(processFileName))
        {
            string localFileName = Path.Combine(Path.GetDirectoryName(processFileName) ?? string.Empty, executable);
            if (File.Exists(localFileName))
            {
                return localFileName;
            }
        }

        return BaseError.New("Unable to locate ErsatzTV.Scanner executable");
    }
}
