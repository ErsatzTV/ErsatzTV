using System.Diagnostics;
using System.Runtime.InteropServices;
using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Metadata;
using ErsatzTV.FFmpeg.Runtime;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using Serilog.Formatting.Compact.Reader;

namespace ErsatzTV.Application.MediaSources;

public class CallLocalLibraryScannerHandler : IRequestHandler<ForceScanLocalLibrary, Either<BaseError, string>>,
    IRequestHandler<ScanLocalLibraryIfNeeded, Either<BaseError, string>>
{
    private readonly IMediator _mediator;
    private readonly IRuntimeInfo _runtimeInfo;
    private readonly ILogger<CallLocalLibraryScannerHandler> _logger;

    public CallLocalLibraryScannerHandler(
        IMediator mediator,
        IRuntimeInfo runtimeInfo,
        ILogger<CallLocalLibraryScannerHandler> logger)
    {
        _mediator = mediator;
        _runtimeInfo = runtimeInfo;
        _logger = logger;
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
        CommandResult process = await Cli.Wrap(scanner)
            .WithArguments(
                new[]
                {
                    "--local",
                    request.LibraryId.ToString()
                })
            .WithValidation(CommandResultValidation.None)
            .WithStandardErrorPipe(PipeTarget.ToDelegate(ProcessLogOutput))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(ProcessProgressOutput))
            .ExecuteAsync(cancellationToken);
        
        if (process.ExitCode != 0)
        {
            return BaseError.New($"ErsatzTV.Scanner exited with code {process.ExitCode}");
        }

        // TODO: return local library name?
        return string.Empty;
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
