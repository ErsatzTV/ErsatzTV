using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Timers;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace ErsatzTV.Application.Streaming;

public class HlsSessionWorkerV2 : IHlsSessionWorker
{
    //private static int _workAheadCount;
    private readonly string _host;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<HlsSessionWorkerV2> _logger;
    private readonly IMediator _mediator;
    private readonly string _scheme;
    private readonly SemaphoreSlim _slim = new(1, 1);
    private readonly Lock _sync = new();
    private readonly Option<int> _targetFramerate;
    private CancellationTokenSource _cancellationTokenSource;
    private string _channelNumber;
    private bool _disposedValue;
    private DateTimeOffset _lastAccess;
    private Option<PlayoutItemProcessModel> _lastProcessModel;
    private IServiceScope _serviceScope;
    private HlsSessionState _state;
    private Timer _timer;
    private DateTimeOffset _transcodedUntil;
    private DateTimeOffset _channelStart;

    public HlsSessionWorkerV2(
        IServiceScopeFactory serviceScopeFactory,
        ILocalFileSystem localFileSystem,
        ILogger<HlsSessionWorkerV2> logger,
        Option<int> targetFramerate,
        string scheme,
        string host)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        _mediator = _serviceScope.ServiceProvider.GetRequiredService<IMediator>();
        _localFileSystem = localFileSystem;
        _logger = logger;
        _targetFramerate = targetFramerate;
        _scheme = scheme;
        _host = host;
    }

    public DateTimeOffset PlaylistStart { get; private set; }

    public async Task Cancel(CancellationToken cancellationToken)
    {
        _logger.LogInformation("API termination request for HLS session for channel {Channel}", _channelNumber);

        await _slim.WaitAsync(cancellationToken);
        try
        {
            await _cancellationTokenSource.CancelAsync();
        }
        finally
        {
            _slim.Release();
        }
    }

    public void Touch()
    {
        lock (_sync)
        {
            //_logger.LogDebug("Keep alive - session worker v2 for channel {ChannelNumber}", _channelNumber);

            _lastAccess = DateTimeOffset.Now;

            _timer?.Stop();
            _timer?.Start();
        }
    }

    public Task<Option<TrimPlaylistResult>> TrimPlaylist(
        DateTimeOffset filterBefore,
        CancellationToken cancellationToken) =>
        Task.FromResult(Option<TrimPlaylistResult>.None);

    public void PlayoutUpdated() => _state = HlsSessionState.PlayoutUpdated;

    public HlsSessionModel GetModel() => new(_channelNumber, _state.ToString(), _transcodedUntil, _lastAccess);

    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task Run(
        string channelNumber,
        Option<TimeSpan> idleTimeout,
        CancellationToken incomingCancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(incomingCancellationToken);

        try
        {
            _channelNumber = channelNumber;

            foreach (var timeout in idleTimeout)
            {
                lock (_sync)
                {
                    _timer = new Timer(timeout.TotalMilliseconds) { AutoReset = false };
                    _timer.Elapsed += CancelRun;
                }
            }

            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            _logger.LogInformation("Starting HLS V2 session for channel {Channel}", channelNumber);

            if (_localFileSystem.ListFiles(Path.Combine(FileSystemLayout.TranscodeFolder, _channelNumber)).Any())
            {
                _logger.LogError("Transcode folder is NOT empty!");
            }

            Touch();
            _transcodedUntil = DateTimeOffset.Now;
            PlaylistStart = _transcodedUntil;
            _channelStart = _transcodedUntil;

            var maybePlayoutId = await _mediator.Send(
                new GetPlayoutIdByChannelNumber(_channelNumber),
                cancellationToken);

            // time shift on-demand playout if needed
            foreach (var playoutId in maybePlayoutId)
            {
                await _mediator.Send(
                    new TimeShiftOnDemandPlayout(playoutId, _transcodedUntil, true),
                    cancellationToken);
            }

            // start concat/segmenter process
            // other transcode processes will be started by incoming requests from concat/segmenter process

            var request = new GetConcatSegmenterProcessByChannelNumber(_scheme, _host, _channelNumber);
            Either<BaseError, PlayoutItemProcessModel> maybeSegmenterProcess =
                await _mediator.Send(request, cancellationToken);

            foreach (BaseError error in maybeSegmenterProcess.LeftToSeq())
            {
                _logger.LogError(
                    "Failed to start concat segmenter for channel {ChannelNumber}: {Error}",
                    _channelNumber,
                    error.ToString());

                return;
            }

            foreach (PlayoutItemProcessModel processModel in maybeSegmenterProcess.RightAsEnumerable())
            {
                Command process = processModel.Process;

                _logger.LogDebug("ffmpeg concat segmenter arguments {FFmpegArguments}", process.Arguments);

                try
                {
                    BufferedCommandResult commandResult = await process
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteBufferedAsync(Encoding.UTF8, cancellationToken);
                }
                catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
                {
                    // TODO: handle result? this will probably *always* be canceled
                    _logger.LogDebug("ffmpeg concat segmenter finished (canceled)");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error in HLS Session Worker V2");
        }
        finally
        {
            if (_timer is not null)
            {
                lock (_sync)
                {
                    _timer.Elapsed -= CancelRun;
                }
            }

            try
            {
                await _mediator.Send(
                    new UpdateOnDemandCheckpoint(_channelNumber, DateTimeOffset.Now),
                    CancellationToken.None);
            }
            catch (Exception)
            {
                // do nothing
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

        return;

        [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods")]
        async void CancelRun(object o, ElapsedEventArgs e)
        {
            try
            {
                await _cancellationTokenSource.CancelAsync();
            }
            catch (Exception)
            {
                // do nothing
            }
        }
    }

    public async Task WaitForPlaylistSegments(
        int initialSegmentCount,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            DateTimeOffset start = DateTimeOffset.Now;
            DateTimeOffset finish = start.AddSeconds(8);

            string segmentFolder = Path.Combine(FileSystemLayout.TranscodeFolder, _channelNumber);
            string playlistFileName = Path.Combine(segmentFolder, "live.m3u8");

            _logger.LogDebug("Waiting for playlist to exist");
            while (!_localFileSystem.FileExists(playlistFileName))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }

            _logger.LogDebug("Playlist exists");

            var segmentCount = 0;
            int lastSegmentCount = -1;
            while (DateTimeOffset.Now < finish && segmentCount < initialSegmentCount)
            {
                if (segmentCount != lastSegmentCount)
                {
                    lastSegmentCount = segmentCount;
                    _logger.LogDebug(
                        "Segment count {SegmentCount} of {InitialSegmentCount}",
                        segmentCount,
                        initialSegmentCount);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);

                segmentCount = _localFileSystem.ListFiles(segmentFolder, "*.ts").Count();
            }
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug("WaitForPlaylistSegments took {Duration}", sw.Elapsed);
        }
    }

    public async Task<Either<BaseError, PlayoutItemProcessModel>> GetNextPlayoutItemProcess()
    {
        foreach (PlayoutItemProcessModel processModel in _lastProcessModel)
        {
            _state = NextState(_state, processModel);
        }

        // if we're at least 30 seconds ahead, drop to realtime
        var transcodedBuffer = TimeSpan.FromSeconds(
            Math.Max(0, _transcodedUntil.Subtract(DateTimeOffset.Now).TotalSeconds));

        if (transcodedBuffer >= TimeSpan.FromSeconds(30))
        {
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

        _logger.LogDebug("Getting next playout item process with state {@State}", _state);

        //long ptsOffset = await GetPtsOffset(_channelNumber, CancellationToken.None);

        bool startAtZero = _state is HlsSessionState.ZeroAndRealtime or HlsSessionState.ZeroAndWorkAhead;
        bool realtime = _state is HlsSessionState.ZeroAndRealtime or HlsSessionState.SeekAndRealtime;

        var request = new GetPlayoutItemProcessByChannelNumber(
            _channelNumber,
            "segmenter-v2",
            _transcodedUntil,
            startAtZero,
            realtime,
            _channelStart,
            0,
            _targetFramerate);

        Either<BaseError, PlayoutItemProcessModel> result = await _mediator.Send(request);

        foreach (PlayoutItemProcessModel processModel in result.RightToSeq())
        {
            _logger.LogDebug("Next playout item process will transcode until {Until}", processModel.Until);

            _transcodedUntil = processModel.Until;
            _lastProcessModel = processModel;
        }

        if (result.IsLeft)
        {
            _lastProcessModel = Option<PlayoutItemProcessModel>.None;
        }

        return result;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (_timer is not null)
                {
                    _timer.Dispose();
                    _timer = null;
                }

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
}
