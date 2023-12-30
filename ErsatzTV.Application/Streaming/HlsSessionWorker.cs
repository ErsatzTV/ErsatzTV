using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Timers;
using Bugsnag;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace ErsatzTV.Application.Streaming;

public class HlsSessionWorker : IHlsSessionWorker
{
    private static readonly SemaphoreSlim Slim = new(1, 1);
    private static int _workAheadCount;
    private readonly IMediator _mediator;
    private readonly IClient _client;
    private readonly IHlsPlaylistFilter _hlsPlaylistFilter;
    private readonly IConfigElementRepository _configElementRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<HlsSessionWorker> _logger;
    private readonly Option<int> _targetFramerate;
    private readonly object _sync = new();
    private string _channelNumber;
    private bool _disposedValue;
    private bool _hasWrittenSegments;
    private DateTimeOffset _lastAccess;
    private DateTimeOffset _lastDelete = DateTimeOffset.MinValue;
    private HlsSessionState _state;
    private Timer _timer;
    private DateTimeOffset _transcodedUntil;
    private IServiceScope _serviceScope;

    public HlsSessionWorker(
        IServiceScopeFactory serviceScopeFactory,
        IClient client,
        IHlsPlaylistFilter hlsPlaylistFilter,
        IConfigElementRepository configElementRepository,
        ILocalFileSystem localFileSystem,
        ILogger<HlsSessionWorker> logger,
        Option<int> targetFramerate)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        _mediator = _serviceScope.ServiceProvider.GetRequiredService<IMediator>();
        _client = client;
        _hlsPlaylistFilter = hlsPlaylistFilter;
        _configElementRepository = configElementRepository;
        _localFileSystem = localFileSystem;
        _logger = logger;
        _targetFramerate = targetFramerate;
    }

    public DateTimeOffset PlaylistStart { get; private set; }

    public void Touch()
    {
        lock (_sync)
        {
            // _logger.LogDebug("Keep alive - session worker for channel {ChannelNumber}", _channelNumber);

            _lastAccess = DateTimeOffset.Now;

            _timer?.Stop();
            _timer?.Start();
        }
    }

    public async Task<Option<TrimPlaylistResult>> TrimPlaylist(
        DateTimeOffset filterBefore,
        CancellationToken cancellationToken)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            await Slim.WaitAsync(cancellationToken);
            try
            {
                Option<string[]> maybeLines = await ReadPlaylistLines(cancellationToken);
                foreach (string[] input in maybeLines)
                {
                    TrimPlaylistResult trimResult = _hlsPlaylistFilter.TrimPlaylist(PlaylistStart, filterBefore, input);
                    if (DateTimeOffset.Now > _lastDelete.AddSeconds(30))
                    {
                        DeleteOldSegments(trimResult);
                        _lastDelete = DateTimeOffset.Now;
                    }

                    return trimResult;
                }
            }
            finally
            {
                Slim.Release();
                sw.Stop();
                // _logger.LogDebug("TrimPlaylist took {Duration}", sw.Elapsed);
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            // do nothing
        }

        return None;
    }

    public void PlayoutUpdated() => _state = HlsSessionState.PlayoutUpdated;

    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task Run(string channelNumber, TimeSpan idleTimeout, CancellationToken incomingCancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(incomingCancellationToken);

        [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods")]
        async void Cancel(object o, ElapsedEventArgs e)
        {
            try
            {
                await cts.CancelAsync();
            }
            catch (Exception)
            {
                // do nothing   
            }
        }

        try
        {
            _channelNumber = channelNumber;

            lock (_sync)
            {
                _timer = new Timer(idleTimeout.TotalMilliseconds) { AutoReset = false };
                _timer.Elapsed += Cancel;
            }

            CancellationToken cancellationToken = cts.Token;

            _logger.LogInformation("Starting HLS session for channel {Channel}", channelNumber);

            if (_localFileSystem.ListFiles(Path.Combine(FileSystemLayout.TranscodeFolder, _channelNumber)).Any())
            {
                _logger.LogError("Transcode folder is NOT empty!");
            }


            Touch();
            _transcodedUntil = DateTimeOffset.Now;
            PlaylistStart = _transcodedUntil;

            bool initialWorkAhead = Volatile.Read(ref _workAheadCount) < await GetWorkAheadLimit();
            _state = initialWorkAhead ? HlsSessionState.SeekAndWorkAhead : HlsSessionState.SeekAndRealtime;

            if (!await Transcode(!initialWorkAhead, cancellationToken))
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                if (DateTimeOffset.Now - _lastAccess > idleTimeout)
                {
                    _logger.LogInformation("Stopping idle HLS session for channel {Channel}", channelNumber);
                    return;
                }

                var transcodedBuffer = TimeSpan.FromSeconds(
                    Math.Max(0, _transcodedUntil.Subtract(DateTimeOffset.Now).TotalSeconds));
                if (transcodedBuffer <= TimeSpan.FromMinutes(1))
                {
                    // only use realtime encoding when we're at least 30 seconds ahead
                    bool realtime = transcodedBuffer >= TimeSpan.FromSeconds(30);
                    bool subsequentWorkAhead =
                        !realtime && Volatile.Read(ref _workAheadCount) < await GetWorkAheadLimit();
                    if (!await Transcode(!subsequentWorkAhead, cancellationToken))
                    {
                        return;
                    }
                }
                else
                {
                    await TrimAndDelete(cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
        }
        finally
        {
            lock (_sync)
            {
                _timer.Elapsed -= Cancel;
            }

            try
            {
                _localFileSystem.EmptyFolder(Path.Combine(FileSystemLayout.TranscodeFolder, _channelNumber));
            }
            catch
            {
                // do nothing
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _timer.Dispose();
                _timer = null;
                
                _serviceScope.Dispose();
                _serviceScope = null;
            }

            _disposedValue = true;
        }
    }

    private HlsSessionState NextState(HlsSessionState state, PlayoutItemProcessModel processModel)
    {
        bool isComplete = processModel?.IsComplete == true;

        HlsSessionState result = state switch
        {
            // playout updates should have the channel start over, transcode method will throttle if needed
            HlsSessionState.PlayoutUpdated => HlsSessionState.SeekAndWorkAhead,

            // after seeking and NOT completing the item, seek again, transcode method will throttle if needed
            HlsSessionState.SeekAndWorkAhead when !isComplete => HlsSessionState.SeekAndWorkAhead,

            // after seeking and completing the item, start at zero
            HlsSessionState.SeekAndWorkAhead => HlsSessionState.ZeroAndWorkAhead,

            // after starting and zero and NOT completing the item, seek, transcode method will throttle if needed
            HlsSessionState.ZeroAndWorkAhead when !isComplete => HlsSessionState.SeekAndWorkAhead,

            // after starting at zero and completing the item, start at zero again, transcode method will throttle if needed
            HlsSessionState.ZeroAndWorkAhead => HlsSessionState.ZeroAndWorkAhead,

            // realtime will always complete items, so start next at zero
            HlsSessionState.SeekAndRealtime => HlsSessionState.ZeroAndRealtime,

            // realtime will always complete items, so start next at zero
            HlsSessionState.ZeroAndRealtime => HlsSessionState.ZeroAndRealtime,

            // this will never happen with the enum
            _ => throw new InvalidOperationException()
        };

        _logger.LogDebug("HLS session state {Last} => {Next}", state, result);

        return result;
    }

    private async Task<bool> Transcode(
        bool realtime,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!realtime)
            {
                Interlocked.Increment(ref _workAheadCount);
                _logger.LogInformation("HLS segmenter will work ahead for channel {Channel}", _channelNumber);
                
                HlsSessionState nextState = _state switch
                {
                    HlsSessionState.SeekAndRealtime => HlsSessionState.SeekAndWorkAhead,
                    HlsSessionState.ZeroAndRealtime => HlsSessionState.ZeroAndWorkAhead,
                    _ => _state
                };
                
                if (nextState != _state)
                {
                    _logger.LogDebug("HLS session state accelerating {Last} => {Next}", _state, nextState);
                    _state = nextState;
                }
            }
            else
            {
                _logger.LogInformation(
                    "HLS segmenter will NOT work ahead for channel {Channel}",
                    _channelNumber);

                // throttle to realtime if needed
                HlsSessionState nextState = _state switch
                {
                    HlsSessionState.SeekAndWorkAhead => HlsSessionState.SeekAndRealtime,
                    HlsSessionState.ZeroAndWorkAhead => HlsSessionState.ZeroAndRealtime,
                    _ => _state
                };

                if (nextState != _state)
                {
                    _logger.LogDebug("HLS session state throttling {Last} => {Next}", _state, nextState);
                    _state = nextState;
                }
            }

            long ptsOffset = await GetPtsOffset(_channelNumber, cancellationToken);
            // _logger.LogInformation("PTS offset: {PtsOffset}", ptsOffset);

            _logger.LogInformation("HLS session state: {State}", _state);

            DateTimeOffset now = _state is HlsSessionState.SeekAndWorkAhead
                ? DateTimeOffset.Now
                : _transcodedUntil.AddSeconds(_state is HlsSessionState.SeekAndRealtime ? 0 : 1);
            bool startAtZero = _state is HlsSessionState.ZeroAndWorkAhead or HlsSessionState.ZeroAndRealtime;

            var request = new GetPlayoutItemProcessByChannelNumber(
                _channelNumber,
                "segmenter",
                now,
                startAtZero,
                realtime,
                ptsOffset,
                _targetFramerate);

            // _logger.LogInformation("Request {@Request}", request);

            Either<BaseError, PlayoutItemProcessModel> result = await _mediator.Send(request, cancellationToken);

            // _logger.LogInformation("Result {Result}", result.ToString());

            foreach (BaseError error in result.LeftAsEnumerable())
            {
                _logger.LogWarning(
                    "Failed to create process for HLS session on channel {Channel}: {Error}",
                    _channelNumber,
                    error.ToString());

                return false;
            }

            foreach (PlayoutItemProcessModel processModel in result.RightAsEnumerable())
            {
                await TrimAndDelete(cancellationToken);

                Command process = processModel.Process;

                _logger.LogInformation("ffmpeg hls arguments {FFmpegArguments}", process.Arguments);

                try
                {
                    BufferedCommandResult commandResult = await process
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteBufferedAsync(Encoding.UTF8, cancellationToken);

                    if (commandResult.ExitCode == 0)
                    {
                        _logger.LogInformation("HLS process has completed for channel {Channel}", _channelNumber);
                        _logger.LogDebug("Transcoded until: {Until}", processModel.Until);
                        _transcodedUntil = processModel.Until;
                        _state = NextState(_state, processModel);
                        _hasWrittenSegments = true;
                        return true;
                    }
                    else
                    {
                        // detect the non-zero exit code and transcode the ffmpeg error message instead

                        string errorMessage = commandResult.StandardError;
                        if (string.IsNullOrWhiteSpace(errorMessage))
                        {
                            errorMessage = $"Unknown FFMPEG error; exit code {commandResult.ExitCode}";
                        }

                        _logger.LogError(
                            "HLS process for channel {Channel} has terminated unsuccessfully with exit code {ExitCode}: {StandardError}",
                            _channelNumber,
                            commandResult.ExitCode,
                            commandResult.StandardError);

                        Either<BaseError, PlayoutItemProcessModel> maybeOfflineProcess = await _mediator.Send(
                            new GetErrorProcess(
                                _channelNumber,
                                "segmenter",
                                realtime,
                                ptsOffset,
                                processModel.MaybeDuration,
                                processModel.Until,
                                errorMessage),
                            cancellationToken);

                        foreach (PlayoutItemProcessModel errorProcessModel in maybeOfflineProcess.RightAsEnumerable())
                        {
                            Command errorProcess = errorProcessModel.Process;

                            _logger.LogInformation(
                                "ffmpeg hls error arguments {FFmpegArguments}",
                                errorProcess.Arguments);

                            commandResult = await errorProcess
                                .WithValidation(CommandResultValidation.None)
                                .ExecuteBufferedAsync(Encoding.UTF8, cancellationToken);

                            if (commandResult.ExitCode == 0)
                            {
                                _transcodedUntil = processModel.Until;
                                _state = NextState(_state, null);

                                _hasWrittenSegments = true;

                                return true;
                            }
                        }

                        return false;
                    }
                }
                catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
                {
                    _logger.LogInformation("Terminating HLS process for channel {Channel}", _channelNumber);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcoding channel {Channel}", _channelNumber);

            try
            {
                _client.Notify(ex);
            }
            catch (Exception)
            {
                // do nothing
            }

            return false;
        }
        finally
        {
            if (!realtime)
            {
                Interlocked.Decrement(ref _workAheadCount);
            }
        }

        return false;
    }

    private async Task TrimAndDelete(CancellationToken cancellationToken)
    {
        await Slim.WaitAsync(cancellationToken);
        try
        {
            Option<string[]> maybeLines = await ReadPlaylistLines(cancellationToken);
            foreach (string[] lines in maybeLines)
            {
                // trim playlist and insert discontinuity before appending with new ffmpeg process
                TrimPlaylistResult trimResult = _hlsPlaylistFilter.TrimPlaylistWithDiscontinuity(
                    PlaylistStart,
                    DateTimeOffset.Now.AddMinutes(-1),
                    lines);
                await WritePlaylist(trimResult.Playlist, cancellationToken);

                DeleteOldSegments(trimResult);

                PlaylistStart = trimResult.PlaylistStart;
            }
        }
        finally
        {
            Slim.Release();
        }
    }

    private void DeleteOldSegments(TrimPlaylistResult trimResult)
    {
        // delete old segments
        var allSegments = Directory.GetFiles(
                Path.Combine(FileSystemLayout.TranscodeFolder, _channelNumber),
                "live*.ts")
            .Map(
                file =>
                {
                    string fileName = Path.GetFileName(file);
                    var sequenceNumber = int.Parse(
                        fileName.Replace("live", string.Empty).Split('.')[0],
                        CultureInfo.InvariantCulture);
                    return new Segment(file, sequenceNumber);
                })
            .ToList();

        var toDelete = allSegments.Filter(s => s.SequenceNumber < trimResult.Sequence).ToList();
        if (toDelete.Count > 0)
        {
            // _logger.LogDebug(
            //     "Deleting HLS segments {Min} to {Max} (less than {StartSequence})",
            //     toDelete.Map(s => s.SequenceNumber).Min(),
            //     toDelete.Map(s => s.SequenceNumber).Max(),
            //     trimResult.Sequence);
        }

        foreach (Segment segment in toDelete)
        {
            File.Delete(segment.File);
        }
    }

    private async Task<long> GetPtsOffset(string channelNumber, CancellationToken cancellationToken)
    {
        await Slim.WaitAsync(cancellationToken);
        try
        {
            long result = 0;

            // if we haven't yet written any segments, start at zero
            if (!_hasWrittenSegments)
            {
                return result;
            }

            Either<BaseError, PtsAndDuration> queryResult = await _mediator.Send(
                new GetLastPtsDuration(channelNumber),
                cancellationToken);

            foreach (BaseError error in queryResult.LeftToSeq())
            {
                _logger.LogWarning("Unable to determine last pts offset - {Error}", error.ToString());
            }

            foreach ((long pts, long duration) in queryResult.RightToSeq())
            {
                result = pts + duration;
            }

            return result;
        }
        finally
        {
            Slim.Release();
        }
    }

    private async Task<int> GetWorkAheadLimit()
    {
        return await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegWorkAheadSegmenters)
            .Map(maybeCount => maybeCount.Match(identity, () => 1));
    }

    private async Task<Option<string[]>> ReadPlaylistLines(CancellationToken cancellationToken)
    {
        string fileName = PlaylistFileName();
        if (File.Exists(fileName))
        {
            return await File.ReadAllLinesAsync(fileName, cancellationToken);
        }

        return None;
    }

    private async Task WritePlaylist(string playlist, CancellationToken cancellationToken)
    {
        string fileName = PlaylistFileName();
        await File.WriteAllTextAsync(fileName, playlist, cancellationToken);
    }

    private string PlaylistFileName() => Path.Combine(
        FileSystemLayout.TranscodeFolder,
        _channelNumber,
        "live.m3u8");

    private sealed record Segment(string File, int SequenceNumber);
}
