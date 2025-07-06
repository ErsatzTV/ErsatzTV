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
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact.Reader;

namespace ErsatzTV.Application.Libraries;

public abstract class CallLibraryScannerHandler<TRequest>
{
    private readonly int _batchSize = 100;
    private readonly ChannelWriter<ISearchIndexBackgroundServiceRequest> _channel;
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IMediator _mediator;
    private readonly IRuntimeInfo _runtimeInfo;
    private readonly List<int> _toReindex = [];
    private readonly List<int> _toRemove = [];
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

            await using CancellationTokenRegistration link =
                cancellationToken.Register(() => forcefulCts.CancelAfter(TimeSpan.FromSeconds(10))
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

            if (_toReindex.Count > 0)
            {
                await _channel.WriteAsync(new ReindexMediaItems(_toReindex.ToArray()), cancellationToken);
                _toReindex.Clear();
            }

            if (_toRemove.Count > 0)
            {
                await _channel.WriteAsync(new RemoveMediaItems(_toReindex.ToArray()), cancellationToken);
                _toRemove.Clear();
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

                ILogger log = Log.Logger;
                if (logEvent.Properties.TryGetValue("SourceContext", out LogEventPropertyValue property))
                {
                    log = log.ForContext(
                        Constants.SourceContextPropertyName,
                        property.ToString().Trim('"'));
                }

                log.Write(
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

                    _toReindex.AddRange(progressUpdate.ItemsToReindex);
                    if (_toReindex.Count >= _batchSize)
                    {
                        await _channel.WriteAsync(new ReindexMediaItems(_toReindex.ToArray()));
                        _toReindex.Clear();
                    }

                    _toRemove.AddRange(progressUpdate.ItemsToRemove);
                    if (_toRemove.Count >= _batchSize)
                    {
                        await _channel.WriteAsync(new RemoveMediaItems(_toReindex.ToArray()));
                        _toRemove.Clear();
                    }

                    if (progressUpdate.PercentComplete is not null)
                    {
                        var progress = new LibraryScanProgress(
                            progressUpdate.LibraryId,
                            progressUpdate.PercentComplete.Value);

                        await _mediator.Publish(progress);
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
        string processExecutable = Path.GetFileNameWithoutExtension(processFileName);
        string folderName = Path.GetDirectoryName(processFileName);
        if ("dotnet".Equals(processExecutable, StringComparison.OrdinalIgnoreCase))
        {
            folderName = AppContext.BaseDirectory;
        }

        if (!string.IsNullOrWhiteSpace(folderName))
        {
            string localFileName = Path.Combine(folderName, executable);
            if (File.Exists(localFileName))
            {
                return localFileName;
            }
        }

        return BaseError.New("Unable to locate ErsatzTV.Scanner executable");
    }
}
