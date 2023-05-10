using System.Runtime.InteropServices;
using System.Threading.Channels;
using CliWrap;
using ErsatzTV.Application.Search;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.MediaSources;
using ErsatzTV.Core.Metadata;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact.Reader;

namespace ErsatzTV.Application.Libraries;

public abstract class CallLibraryScannerHandler<TRequest>
{
    private readonly ChannelWriter<ISearchIndexBackgroundServiceRequest> _channel;
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediator _mediator;
    private readonly IRuntimeInfo _runtimeInfo;
    private string _libraryName;

    protected CallLibraryScannerHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IConfigElementRepository configElementRepository,
        ChannelWriter<ISearchIndexBackgroundServiceRequest> channel,
        IMediator mediator,
        IRuntimeInfo runtimeInfo)
    {
        _dbContextFactory = dbContextFactory;
        _configElementRepository = configElementRepository;
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
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
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
                // make a new log event to force using local time
                // because the compact json writer used by the scanner
                // writes in UTC
                LogEvent logEvent = LogEventReader.ReadFromString(s);
                Log.Write(
                    new LogEvent(
                        logEvent.Timestamp.ToLocalTime(),
                        logEvent.Level,
                        logEvent.Exception,
                        logEvent.MessageTemplate,
                        logEvent.Properties.Map(pair => new LogEventProperty(pair.Key, pair.Value))));
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

    protected abstract Task<DateTimeOffset> GetLastScan(TvContext dbContext, TRequest request);
    protected abstract bool ScanIsRequired(DateTimeOffset lastScan, int libraryRefreshInterval, TRequest request);

    protected async Task<Validation<BaseError, string>> Validate(TRequest request)
    {
        int libraryRefreshInterval = await _configElementRepository
            .GetValue<int>(ConfigElementKey.LibraryRefreshInterval)
            .IfNoneAsync(0);

        libraryRefreshInterval = Math.Clamp(libraryRefreshInterval, 0, 999_999);

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        DateTimeOffset lastScan = await GetLastScan(dbContext, request);
        if (!ScanIsRequired(lastScan, libraryRefreshInterval, request))
        {
            return new ScanIsNotRequired();
        }

        string executable = _runtimeInfo.IsOSPlatform(OSPlatform.Windows)
            ? "ErsatzTV.Scanner.exe"
            : "ErsatzTV.Scanner";

        string processFileName = Environment.ProcessPath ?? string.Empty;
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
