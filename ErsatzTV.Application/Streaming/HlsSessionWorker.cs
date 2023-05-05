using System.Diagnostics;
using System.Text;
using System.Timers;
using Bugsnag;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Application.Channels;
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
    private readonly IHlsPlaylistFilter _hlsPlaylistFilter;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<HlsSessionWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly object _sync = new();
    private string _channelNumber;
    private bool _firstProcess;
    private bool _hasWrittenSegments;
    private DateTimeOffset _lastAccess;
    private DateTimeOffset _lastDelete = DateTimeOffset.MinValue;
    private bool _seekNextItem;
    private Option<int> _targetFramerate;
    private Timer _timer;
    private DateTimeOffset _transcodedUntil;

    public HlsSessionWorker(
        IHlsPlaylistFilter hlsPlaylistFilter,
        IServiceScopeFactory serviceScopeFactory,
        ILocalFileSystem localFileSystem,
        ILogger<HlsSessionWorker> logger)
    {
        _hlsPlaylistFilter = hlsPlaylistFilter;
        _serviceScopeFactory = serviceScopeFactory;
        _localFileSystem = localFileSystem;
        _logger = logger;
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

    public void PlayoutUpdated()
    {
        _firstProcess = true;
        _seekNextItem = true;
    }

    public async Task Run(string channelNumber, TimeSpan idleTimeout, CancellationToken incomingCancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(incomingCancellationToken);

        void Cancel(object o, ElapsedEventArgs e)
        {
            cts.Cancel();
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

            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            ILocalFileSystem localFileSystem = scope.ServiceProvider.GetRequiredService<ILocalFileSystem>();
            if (localFileSystem.ListFiles(Path.Combine(FileSystemLayout.TranscodeFolder, _channelNumber)).Any())
            {
                _logger.LogError("Transcode folder is NOT empty!");
            }

            _targetFramerate = await mediator.Send(
                new GetChannelFramerate(channelNumber),
                cancellationToken);

            Touch();
            _transcodedUntil = DateTimeOffset.Now;
            PlaylistStart = _transcodedUntil;

            _firstProcess = true;

            bool initialWorkAhead = Volatile.Read(ref _workAheadCount) < await GetWorkAheadLimit();
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

    private async Task<bool> Transcode(
        bool realtime,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        try
        {
            if (!realtime)
            {
                Interlocked.Increment(ref _workAheadCount);
                _logger.LogInformation("HLS segmenter will work ahead for channel {Channel}", _channelNumber);
            }
            else
            {
                _logger.LogInformation(
                    "HLS segmenter will NOT work ahead for channel {Channel}",
                    _channelNumber);
            }

            IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            long ptsOffset = await GetPtsOffset(mediator, _channelNumber, cancellationToken);
            // _logger.LogInformation("PTS offset: {PtsOffset}", ptsOffset);

            var request = new GetPlayoutItemProcessByChannelNumber(
                _channelNumber,
                "segmenter",
                _firstProcess ? DateTimeOffset.Now : _transcodedUntil.AddSeconds(1),
                !_firstProcess,
                realtime,
                ptsOffset,
                _targetFramerate);

            // _logger.LogInformation("Request {@Request}", request);

            Either<BaseError, PlayoutItemProcessModel> result = await mediator.Send(request, cancellationToken);

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
                        _transcodedUntil = processModel.Until;
                        _firstProcess = false;
                        if (_seekNextItem)
                        {
                            _firstProcess = true;
                            _seekNextItem = false;
                        }

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

                        Either<BaseError, PlayoutItemProcessModel> maybeOfflineProcess = await mediator.Send(
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
                                _firstProcess = false;
                                if (_seekNextItem)
                                {
                                    _firstProcess = true;
                                    _seekNextItem = false;
                                }

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
                IClient client = scope.ServiceProvider.GetRequiredService<IClient>();
                client.Notify(ex);
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
                    var sequenceNumber = int.Parse(fileName.Replace("live", string.Empty).Split('.')[0]);
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

    private async Task<long> GetPtsOffset(
        IMediator mediator,
        string channelNumber,
        CancellationToken cancellationToken)
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

            Either<BaseError, PtsAndDuration> queryResult = await mediator.Send(
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
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IConfigElementRepository repo = scope.ServiceProvider.GetRequiredService<IConfigElementRepository>();
        return await repo.GetValue<int>(ConfigElementKey.FFmpegWorkAheadSegmenters)
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

    private record Segment(string File, int SequenceNumber);
}
