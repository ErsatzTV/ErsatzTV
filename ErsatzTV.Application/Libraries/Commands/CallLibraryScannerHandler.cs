using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using CliWrap;
using ErsatzTV.Application.Search;
using ErsatzTV.Core;
using ErsatzTV.Core.MediaSources;
using ErsatzTV.Core.Metadata;
using ErsatzTV.FFmpeg.Runtime;
using Newtonsoft.Json;
using Serilog;
using Serilog.Formatting.Compact.Reader;

namespace ErsatzTV.Application.Libraries;

public abstract class CallLibraryScannerHandler
{
    private readonly ChannelWriter<ISearchIndexBackgroundServiceRequest> _channel;
    private readonly IMediator _mediator;
    private readonly IRuntimeInfo _runtimeInfo;
    private string _libraryName;

    protected CallLibraryScannerHandler(
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IMediator mediator,
        IRuntimeInfo runtimeInfo)
    {
        _channel = channel;
        _mediator = mediator;
        _runtimeInfo = runtimeInfo;
    }

    protected async Task<Either<BaseError, string>> PerformScan(
        string scanner,
        List<string> arguments,
        CancellationToken cancellationToken)
    {
        try
        {
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
        }
        catch (OperationCanceledException)
        {
            // do nothing
        }

        return _libraryName ?? string.Empty;
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
                ScannerProgressUpdate progressUpdate = JsonConvert.DeserializeObject<ScannerProgressUpdate>(s);
                if (progressUpdate != null)
                {
                    if (!string.IsNullOrWhiteSpace(progressUpdate.LibraryName))
                    {
                        _libraryName = progressUpdate.LibraryName;
                    }

                    if (progressUpdate.PercentComplete is not null)
                    {
                        var progress = new LibraryScanProgress(
                            progressUpdate.LibraryId,
                            progressUpdate.PercentComplete.Value);

                        await _mediator.Publish(progress);
                    }

                    if (progressUpdate.ItemsToReindex.Length > 0)
                    {
                        var reindex = new ReindexMediaItems(progressUpdate.ItemsToReindex);
                        await _channel.WriteAsync(reindex);
                    }

                    if (progressUpdate.ItemsToRemove.Length > 0)
                    {
                        var remove = new RemoveMediaItems(progressUpdate.ItemsToRemove);
                        await _channel.WriteAsync(remove);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex, "Unable to process scanner progress update");
            }
        }
    }

    protected Validation<BaseError, string> Validate()
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
