﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Timers;
using Bugsnag;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Application.Playouts;
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
    private static int _workAheadCount;
    private readonly IClient _client;
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IHlsPlaylistFilter _hlsPlaylistFilter;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<HlsSessionWorker> _logger;
    private readonly IMediator _mediator;
    private readonly SemaphoreSlim _slim = new(1, 1);
    private readonly object _sync = new();
    private readonly Option<int> _targetFramerate;
    private CancellationTokenSource _cancellationTokenSource;
    private string _channelNumber;
    private bool _disposedValue;
    private bool _hasWrittenSegments;
    private DateTimeOffset _lastAccess;
    private DateTimeOffset _lastDelete = DateTimeOffset.MinValue;
    private IServiceScope _serviceScope;
    private HlsSessionState _state;
    private Timer _timer;
    private DateTimeOffset _transcodedUntil;

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
            await _slim.WaitAsync(cancellationToken);
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

                _logger.LogWarning("HlsSessionWorker.TrimPlaylist read empty playlist?");
            }
            finally
            {
                _slim.Release();
                sw.Stop();
                // _logger.LogDebug("TrimPlaylist took {Duration}", sw.Elapsed);
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            // do nothing
            _logger.LogDebug("HlsSessionWorker.TrimPlaylist was canceled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error trimming playlist");
        }

        return None;
    }

    public void PlayoutUpdated() => _state = HlsSessionState.PlayoutUpdated;

    public HlsSessionModel GetModel() => new(_channelNumber, _state.ToString(), _transcodedUntil, _lastAccess);

    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task Run(string channelNumber, TimeSpan idleTimeout, CancellationToken incomingCancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(incomingCancellationToken);

        try
        {
            _channelNumber = channelNumber;

            lock (_sync)
            {
                _timer = new Timer(idleTimeout.TotalMilliseconds) { AutoReset = false };
                _timer.Elapsed += CancelRun;
            }

            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            _logger.LogInformation("Starting HLS session for channel {Channel}", channelNumber);

            if (_localFileSystem.ListFiles(Path.Combine(FileSystemLayout.TranscodeFolder, _channelNumber)).Any())
            {
                _logger.LogError("Transcode folder is NOT empty!");
            }

            Touch();
            _transcodedUntil = DateTimeOffset.Now;
            PlaylistStart = _transcodedUntil;

            // time shift on-demand playout if needed
            await _mediator.Send(new TimeShiftOnDemandPlayout(_channelNumber, _transcodedUntil, true), cancellationToken);

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
                _timer.Elapsed -= CancelRun;
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
        _logger.LogDebug("Waiting for playlist segments...");

        var sw = Stopwatch.StartNew();
        try
        {
            DateTimeOffset start = DateTimeOffset.Now;
            DateTimeOffset finish = start.AddSeconds(8);

            string playlistFileName = Path.Combine(FileSystemLayout.TranscodeFolder, _channelNumber, "live.m3u8");

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

                DateTimeOffset now = DateTimeOffset.Now.AddSeconds(-30);
                Option<TrimPlaylistResult> maybeResult = await TrimPlaylist(now, cancellationToken);
                foreach (TrimPlaylistResult result in maybeResult)
                {
                    segmentCount = result.SegmentCount;
                }
            }
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug("WaitForPlaylistSegments took {Duration}", sw.Elapsed);
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
                _logger.LogDebug("HLS segmenter will work ahead for channel {Channel}", _channelNumber);

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
                _logger.LogDebug(
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

            _logger.LogDebug("HLS session state: {State}", _state);

            DateTimeOffset now = _state is HlsSessionState.SeekAndWorkAhead ? DateTimeOffset.Now : _transcodedUntil;
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

                _logger.LogDebug("ffmpeg hls arguments {FFmpegArguments}", process.Arguments);

                try
                {
                    BufferedCommandResult commandResult = await process
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteBufferedAsync(Encoding.UTF8, cancellationToken);

                    if (commandResult.ExitCode == 0)
                    {
                        _logger.LogDebug("HLS process has completed for channel {Channel}", _channelNumber);
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

                            _logger.LogDebug(
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
                    _logger.LogInformation("Terminating HLS session for channel {Channel}", _channelNumber);
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

            if (!realtime)
            {
                Interlocked.Decrement(ref _workAheadCount);
            }
        }

        return false;
    }

    private async Task TrimAndDelete(CancellationToken cancellationToken)
    {
        await _slim.WaitAsync(cancellationToken);
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
            _slim.Release();
        }
    }

    private void DeleteOldSegments(TrimPlaylistResult trimResult)
    {
        // delete old segments
        var allSegments = Directory.GetFiles(
                Path.Combine(FileSystemLayout.TranscodeFolder, _channelNumber),
                "live*.ts").Append(
                Directory.GetFiles(
                    Path.Combine(FileSystemLayout.TranscodeFolder, _channelNumber),
                    "live*.mp4"))
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
            try
            {
                File.Delete(segment.File);
            }
            catch (IOException)
            {
                // work around lots of:
                //   The process cannot access the file '...' because it is being used by another process
                _logger.LogDebug("Failed to delete old segment {File}", segment.File);
            }
        }
    }

    private async Task<long> GetPtsOffset(string channelNumber, CancellationToken cancellationToken)
    {
        await _slim.WaitAsync(cancellationToken);
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
                result = pts + duration + 1;
            }

            return result;
        }
        finally
        {
            _slim.Release();
        }
    }

    private async Task<int> GetWorkAheadLimit() =>
        await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegWorkAheadSegmenters)
            .Map(maybeCount => maybeCount.Match(identity, () => 1));

    private async Task<Option<string[]>> ReadPlaylistLines(CancellationToken cancellationToken)
    {
        string fileName = PlaylistFileName();
        if (File.Exists(fileName))
        {
            return await File.ReadAllLinesAsync(fileName, cancellationToken);
        }

        _logger.LogDebug("Playlist does not exist at expected location {File}", fileName);
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
