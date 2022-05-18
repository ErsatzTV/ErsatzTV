using CliWrap;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;
using MediaStream = ErsatzTV.Core.Domain.MediaStream;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegLibraryProcessService : IFFmpegProcessService
{
    private readonly FFmpegProcessService _ffmpegProcessService;
    private readonly IFFmpegStreamSelector _ffmpegStreamSelector;
    private readonly ILogger<FFmpegLibraryProcessService> _logger;
    private readonly FFmpegPlaybackSettingsCalculator _playbackSettingsCalculator;
    private readonly ITempFilePool _tempFilePool;

    public FFmpegLibraryProcessService(
        FFmpegProcessService ffmpegProcessService,
        FFmpegPlaybackSettingsCalculator playbackSettingsCalculator,
        IFFmpegStreamSelector ffmpegStreamSelector,
        ITempFilePool tempFilePool,
        ILogger<FFmpegLibraryProcessService> logger)
    {
        _ffmpegProcessService = ffmpegProcessService;
        _playbackSettingsCalculator = playbackSettingsCalculator;
        _ffmpegStreamSelector = ffmpegStreamSelector;
        _tempFilePool = tempFilePool;
        _logger = logger;
    }

    public async Task<Command> ForPlayoutItem(
        string ffmpegPath,
        string ffprobePath,
        bool saveReports,
        Channel channel,
        MediaVersion videoVersion,
        MediaVersion audioVersion,
        string videoPath,
        string audioPath,
        List<Subtitle> subtitles,
        string preferredAudioLanguage,
        string preferredSubtitleLanguage,
        ChannelSubtitleMode subtitleMode,
        DateTimeOffset start,
        DateTimeOffset finish,
        DateTimeOffset now,
        Option<ChannelWatermark> playoutItemWatermark,
        Option<ChannelWatermark> globalWatermark,
        VaapiDriver vaapiDriver,
        string vaapiDevice,
        bool hlsRealtime,
        FillerKind fillerKind,
        TimeSpan inPoint,
        TimeSpan outPoint,
        long ptsOffset,
        Option<int> targetFramerate,
        bool disableWatermarks)
    {
        MediaStream videoStream = await _ffmpegStreamSelector.SelectVideoStream(videoVersion);
        Option<MediaStream> maybeAudioStream =
            await _ffmpegStreamSelector.SelectAudioStream(
                audioVersion,
                channel.StreamingMode,
                channel.Number,
                preferredAudioLanguage);
        Option<Subtitle> maybeSubtitle =
            await _ffmpegStreamSelector.SelectSubtitleStream(
                videoVersion,
                subtitles,
                channel.StreamingMode,
                channel.Number,
                preferredSubtitleLanguage,
                subtitleMode);

        FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.CalculateSettings(
            channel.StreamingMode,
            channel.FFmpegProfile,
            videoVersion,
            videoStream,
            maybeAudioStream,
            start,
            now,
            inPoint,
            outPoint,
            hlsRealtime,
            targetFramerate);

        Option<WatermarkOptions> watermarkOptions = disableWatermarks
            ? None
            : await _ffmpegProcessService.GetWatermarkOptions(
                ffprobePath,
                channel,
                playoutItemWatermark,
                globalWatermark,
                videoVersion,
                None,
                None);

        Option<List<FadePoint>> maybeFadePoints = watermarkOptions
            .Map(o => o.Watermark)
            .Flatten()
            .Where(wm => wm.Mode == ChannelWatermarkMode.Intermittent)
            .Map(
                wm =>
                    WatermarkCalculator.CalculateFadePoints(
                        start,
                        inPoint,
                        outPoint,
                        playbackSettings.StreamSeek,
                        wm.FrequencyMinutes,
                        wm.DurationSeconds));

        string audioFormat = playbackSettings.AudioFormat switch
        {
            FFmpegProfileAudioFormat.Aac => AudioFormat.Aac,
            FFmpegProfileAudioFormat.Ac3 => AudioFormat.Ac3,
            FFmpegProfileAudioFormat.Copy => AudioFormat.Copy,
            _ => throw new ArgumentOutOfRangeException($"unexpected audio format {playbackSettings.VideoFormat}")
        };

        var audioState = new AudioState(
            audioFormat,
            playbackSettings.AudioChannels,
            playbackSettings.AudioBitrate,
            playbackSettings.AudioBufferSize,
            playbackSettings.AudioSampleRate,
            videoPath == audioPath ? playbackSettings.AudioDuration : Option<TimeSpan>.None,
            playbackSettings.NormalizeLoudness);

        var ffmpegVideoStream = new VideoStream(
            videoStream.Index,
            videoStream.Codec,
            AvailablePixelFormats.ForPixelFormat(videoStream.PixelFormat, _logger),
            new FrameSize(videoVersion.Width, videoVersion.Height),
            videoVersion.RFrameRate,
            videoPath != audioPath); // still image when paths are different

        var videoInputFile = new VideoInputFile(videoPath, new List<VideoStream> { ffmpegVideoStream });

        Option<AudioInputFile> audioInputFile = maybeAudioStream.Map(
            audioStream =>
            {
                var ffmpegAudioStream = new AudioStream(audioStream.Index, audioStream.Codec, audioStream.Channels);
                return new AudioInputFile(audioPath, new List<AudioStream> { ffmpegAudioStream }, audioState);
            });

        Option<SubtitleInputFile> subtitleInputFile = maybeSubtitle.Map<Option<SubtitleInputFile>>(
            subtitle =>
            {
                if (!subtitle.IsImage && subtitle.SubtitleKind == SubtitleKind.Embedded && !subtitle.IsExtracted)
                {
                    _logger.LogWarning("Subtitles are not yet available for this item");
                    return None;
                }

                var ffmpegSubtitleStream = new ErsatzTV.FFmpeg.MediaStream(
                    subtitle.IsImage ? subtitle.StreamIndex : 0,
                    subtitle.Codec,
                    StreamKind.Video);

                string path = subtitle.IsImage
                    ? videoPath
                    : Path.Combine(FileSystemLayout.SubtitleCacheFolder, subtitle.Path);

                return new SubtitleInputFile(
                    path,
                    new List<ErsatzTV.FFmpeg.MediaStream> { ffmpegSubtitleStream },
                    false);

                // TODO: figure out HLS direct
                // channel.StreamingMode == StreamingMode.HttpLiveStreamingDirect);
            }).Flatten();

        Option<WatermarkInputFile> watermarkInputFile = GetWatermarkInputFile(watermarkOptions, maybeFadePoints);

        string videoFormat = GetVideoFormat(playbackSettings);

        HardwareAccelerationMode hwAccel = playbackSettings.HardwareAcceleration switch
        {
            HardwareAccelerationKind.Nvenc => HardwareAccelerationMode.Nvenc,
            HardwareAccelerationKind.Qsv => HardwareAccelerationMode.Qsv,
            HardwareAccelerationKind.Vaapi => HardwareAccelerationMode.Vaapi,
            HardwareAccelerationKind.VideoToolbox => HardwareAccelerationMode.VideoToolbox,
            _ => HardwareAccelerationMode.None
        };

        OutputFormatKind outputFormat = channel.StreamingMode == StreamingMode.HttpLiveStreamingSegmenter
            ? OutputFormatKind.Hls
            : OutputFormatKind.MpegTs;

        Option<string> hlsPlaylistPath = outputFormat == OutputFormatKind.Hls
            ? Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live.m3u8")
            : Option<string>.None;

        Option<string> hlsSegmentTemplate = outputFormat == OutputFormatKind.Hls
            ? Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live%06d.ts")
            : Option<string>.None;

        // normalize songs to yuv420p
        Option<IPixelFormat> desiredPixelFormat =
            videoPath == audioPath ? ffmpegVideoStream.PixelFormat : new PixelFormatYuv420P();

        var desiredState = new FrameState(
            playbackSettings.RealtimeOutput,
            false, // TODO: fallback filler needs to loop
            videoFormat,
            desiredPixelFormat,
            await playbackSettings.ScaledSize.Map(ss => new FrameSize(ss.Width, ss.Height))
                .IfNoneAsync(new FrameSize(videoVersion.Width, videoVersion.Height)),
            new FrameSize(channel.FFmpegProfile.Resolution.Width, channel.FFmpegProfile.Resolution.Height),
            playbackSettings.FrameRate,
            playbackSettings.VideoBitrate,
            playbackSettings.VideoBufferSize,
            playbackSettings.VideoTrackTimeScale,
            playbackSettings.Deinterlace);

        var ffmpegState = new FFmpegState(
            saveReports,
            hwAccel,
            VaapiDriverName(hwAccel, vaapiDriver),
            VaapiDeviceName(hwAccel, vaapiDevice),
            playbackSettings.StreamSeek,
            finish - now,
            channel.StreamingMode != StreamingMode.HttpLiveStreamingDirect,
            "ErsatzTV",
            channel.Name,
            maybeAudioStream.Map(s => Optional(s.Language)).Flatten(),
            outputFormat,
            hlsPlaylistPath,
            hlsSegmentTemplate,
            ptsOffset,
            playbackSettings.ThreadCount);

        _logger.LogDebug("FFmpeg desired state {FrameState}", desiredState);

        var pipelineBuilder = new PipelineBuilder(
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            subtitleInputFile,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            _logger);

        FFmpegPipeline pipeline = pipelineBuilder.Build(ffmpegState, desiredState);

        return GetCommand(ffmpegPath, videoInputFile, audioInputFile, watermarkInputFile, None, pipeline);
    }

    public async Task<Command> ForError(
        string ffmpegPath,
        Channel channel,
        Option<TimeSpan> duration,
        string errorMessage,
        bool hlsRealtime,
        long ptsOffset)
    {
        FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.CalculateErrorSettings(
            channel.StreamingMode,
            channel.FFmpegProfile,
            hlsRealtime);

        IDisplaySize desiredResolution = channel.FFmpegProfile.Resolution;

        var fontSize = (int)Math.Round(channel.FFmpegProfile.Resolution.Height / 20.0);
        var margin = (int)Math.Round(channel.FFmpegProfile.Resolution.Height * 0.05);

        string subtitleFile = await new SubtitleBuilder(_tempFilePool)
            .WithResolution(desiredResolution)
            .WithFontName("Roboto")
            .WithFontSize(fontSize)
            .WithAlignment(2)
            .WithMarginV(margin)
            .WithPrimaryColor("&HFFFFFF")
            .WithFormattedContent(errorMessage.Replace(Environment.NewLine, "\\N"))
            .BuildFile();

        string audioFormat = playbackSettings.AudioFormat switch
        {
            FFmpegProfileAudioFormat.Ac3 => AudioFormat.Ac3,
            _ => AudioFormat.Aac
        };

        var audioState = new AudioState(
            audioFormat,
            playbackSettings.AudioChannels,
            playbackSettings.AudioBitrate,
            playbackSettings.AudioBufferSize,
            playbackSettings.AudioSampleRate,
            Option<TimeSpan>.None,
            false);

        var desiredState = new FrameState(
            playbackSettings.RealtimeOutput,
            false,
            GetVideoFormat(playbackSettings),
            new PixelFormatYuv420P(),
            new FrameSize(desiredResolution.Width, desiredResolution.Height),
            new FrameSize(desiredResolution.Width, desiredResolution.Height),
            playbackSettings.FrameRate,
            playbackSettings.VideoBitrate,
            playbackSettings.VideoBufferSize,
            playbackSettings.VideoTrackTimeScale,
            playbackSettings.Deinterlace);

        OutputFormatKind outputFormat = channel.StreamingMode == StreamingMode.HttpLiveStreamingSegmenter
            ? OutputFormatKind.Hls
            : OutputFormatKind.MpegTs;

        Option<string> hlsPlaylistPath = outputFormat == OutputFormatKind.Hls
            ? Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live.m3u8")
            : Option<string>.None;

        Option<string> hlsSegmentTemplate = outputFormat == OutputFormatKind.Hls
            ? Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live%06d.ts")
            : Option<string>.None;

        string videoPath = Path.Combine(FileSystemLayout.ResourcesCacheFolder, "background.png");

        var videoVersion = BackgroundImageMediaVersion.ForPath(videoPath, desiredResolution);

        var ffmpegVideoStream = new VideoStream(
            0,
            VideoFormat.GeneratedImage,
            new PixelFormatUnknown(), // leave this unknown so we convert to desired yuv420p
            new FrameSize(videoVersion.Width, videoVersion.Height),
            None,
            true);

        var videoInputFile = new VideoInputFile(videoPath, new List<VideoStream> { ffmpegVideoStream });

        var ffmpegState = new FFmpegState(
            false,
            HardwareAccelerationMode.None,
            None,
            None,
            playbackSettings.StreamSeek,
            duration,
            channel.StreamingMode != StreamingMode.HttpLiveStreamingDirect,
            "ErsatzTV",
            channel.Name,
            None,
            outputFormat,
            hlsPlaylistPath,
            hlsSegmentTemplate,
            ptsOffset,
            Option<int>.None);

        var ffmpegSubtitleStream = new ErsatzTV.FFmpeg.MediaStream(0, "ass", StreamKind.Video);

        var audioInputFile = new NullAudioInputFile(audioState);

        var subtitleInputFile = new SubtitleInputFile(
            subtitleFile,
            new List<ErsatzTV.FFmpeg.MediaStream> { ffmpegSubtitleStream },
            false);

        _logger.LogDebug("FFmpeg desired error state {FrameState}", desiredState);

        var pipelineBuilder = new PipelineBuilder(
            videoInputFile,
            audioInputFile,
            None,
            subtitleInputFile,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            _logger);

        FFmpegPipeline pipeline = pipelineBuilder.Build(ffmpegState, desiredState);

        return GetCommand(ffmpegPath, videoInputFile, audioInputFile, None, None, pipeline);
    }

    public Command ConcatChannel(string ffmpegPath, bool saveReports, Channel channel, string scheme, string host)
    {
        var resolution = new FrameSize(channel.FFmpegProfile.Resolution.Width, channel.FFmpegProfile.Resolution.Height);

        var concatInputFile = new ConcatInputFile(
            $"http://localhost:{Settings.ListenPort}/ffmpeg/concat/{channel.Number}",
            resolution);

        var pipelineBuilder = new PipelineBuilder(
            None,
            None,
            None,
            None,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            _logger);

        FFmpegPipeline pipeline = pipelineBuilder.Concat(
            concatInputFile,
            FFmpegState.Concat(saveReports, channel.Name));

        return GetCommand(ffmpegPath, None, None, None, concatInputFile, pipeline);
    }

    public Command WrapSegmenter(string ffmpegPath, bool saveReports, Channel channel, string scheme, string host) =>
        _ffmpegProcessService.WrapSegmenter(ffmpegPath, saveReports, channel, scheme, host);

    public Command ResizeImage(string ffmpegPath, string inputFile, string outputFile, int height)
    {
        var videoInputFile = new VideoInputFile(
            inputFile,
            new List<VideoStream> { new(0, string.Empty, None, FrameSize.Unknown, None, true) });

        var pipelineBuilder = new PipelineBuilder(
            videoInputFile,
            None,
            None,
            None,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            _logger);

        FFmpegPipeline pipeline = pipelineBuilder.Resize(outputFile, new FrameSize(-1, height));

        return GetCommand(ffmpegPath, videoInputFile, None, None, None, pipeline, false);
    }

    public Command ConvertToPng(string ffmpegPath, string inputFile, string outputFile) =>
        _ffmpegProcessService.ConvertToPng(ffmpegPath, inputFile, outputFile);

    public Command ExtractAttachedPicAsPng(string ffmpegPath, string inputFile, int streamIndex, string outputFile) =>
        _ffmpegProcessService.ExtractAttachedPicAsPng(ffmpegPath, inputFile, streamIndex, outputFile);

    public Task<Either<BaseError, string>> GenerateSongImage(
        string ffmpegPath,
        string ffprobePath,
        Option<string> subtitleFile,
        Channel channel,
        Option<ChannelWatermark> playoutItemWatermark,
        Option<ChannelWatermark> globalWatermark,
        MediaVersion videoVersion,
        string videoPath,
        bool boxBlur,
        Option<string> watermarkPath,
        WatermarkLocation watermarkLocation,
        int horizontalMarginPercent,
        int verticalMarginPercent,
        int watermarkWidthPercent,
        CancellationToken cancellationToken) =>
        _ffmpegProcessService.GenerateSongImage(
            ffmpegPath,
            ffprobePath,
            subtitleFile,
            channel,
            playoutItemWatermark,
            globalWatermark,
            videoVersion,
            videoPath,
            boxBlur,
            watermarkPath,
            watermarkLocation,
            horizontalMarginPercent,
            verticalMarginPercent,
            watermarkWidthPercent,
            cancellationToken);

    private Option<WatermarkInputFile> GetWatermarkInputFile(
        Option<WatermarkOptions> watermarkOptions,
        Option<List<FadePoint>> maybeFadePoints)
    {
        foreach (WatermarkOptions options in watermarkOptions)
        {
            foreach (ChannelWatermark watermark in options.Watermark)
            {
                // skip watermark if intermittent and no fade points
                if (watermark.Mode != ChannelWatermarkMode.None &&
                    (watermark.Mode != ChannelWatermarkMode.Intermittent ||
                     maybeFadePoints.Map(fp => fp.Count > 0).IfNone(false)))
                {
                    foreach (string path in options.ImagePath)
                    {
                        var watermarkInputFile = new WatermarkInputFile(
                            path,
                            new List<VideoStream>
                            {
                                new(
                                    options.ImageStreamIndex.IfNone(0),
                                    "unknown",
                                    new PixelFormatUnknown(),
                                    new FrameSize(1, 1),
                                    Option<string>.None,
                                    !options.IsAnimated)
                            },
                            new WatermarkState(
                                maybeFadePoints.Map(
                                    lst => lst.Map(
                                        fp =>
                                        {
                                            return fp switch
                                            {
                                                FadeInPoint fip => (WatermarkFadePoint)new WatermarkFadeIn(
                                                    fip.Time,
                                                    fip.EnableStart,
                                                    fip.EnableFinish),
                                                FadeOutPoint fop => new WatermarkFadeOut(
                                                    fop.Time,
                                                    fop.EnableStart,
                                                    fop.EnableFinish),
                                                _ => throw new NotSupportedException() // this will never happen
                                            };
                                        }).ToList()),
                                watermark.Location,
                                watermark.Size,
                                watermark.WidthPercent,
                                watermark.HorizontalMarginPercent,
                                watermark.VerticalMarginPercent,
                                watermark.Opacity));

                        return watermarkInputFile;
                    }
                }
            }
        }

        return None;
    }

    private Command GetCommand(
        string ffmpegPath,
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<ConcatInputFile> concatInputFile,
        FFmpegPipeline pipeline,
        bool log = true)
    {
        IEnumerable<string> loggedSteps = pipeline.PipelineSteps.Map(ps => ps.GetType().Name);
        IEnumerable<string> loggedVideoFilters =
            videoInputFile.Map(f => f.FilterSteps.Map(vf => vf.GetType().Name)).Flatten();
        IEnumerable<string> loggedAudioFilters =
            audioInputFile.Map(f => f.FilterSteps.Map(af => af.GetType().Name)).Flatten();

        if (log)
        {
            _logger.LogDebug(
                "FFmpeg pipeline {PipelineSteps}, {AudioFilters}, {VideoFilters}",
                loggedSteps,
                loggedAudioFilters,
                loggedVideoFilters
            );
        }

        IList<EnvironmentVariable> environmentVariables =
            CommandGenerator.GenerateEnvironmentVariables(pipeline.PipelineSteps);
        IList<string> arguments = CommandGenerator.GenerateArguments(
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            concatInputFile,
            pipeline.PipelineSteps);

        if (environmentVariables.Any())
        {
            _logger.LogDebug("FFmpeg environment variables {EnvVars}", environmentVariables);
        }

        return Cli.Wrap(ffmpegPath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .WithStandardErrorPipe(PipeTarget.ToStream(Stream.Null))
            .WithEnvironmentVariables(environmentVariables.ToDictionary(e => e.Key, e => e.Value));
    }

    private static Option<string> VaapiDriverName(HardwareAccelerationMode accelerationMode, VaapiDriver driver)
    {
        if (accelerationMode == HardwareAccelerationMode.Vaapi)
        {
            switch (driver)
            {
                case VaapiDriver.i965:
                    return "i965";
                case VaapiDriver.iHD:
                    return "iHD";
                case VaapiDriver.RadeonSI:
                    return "radeonsi";
                case VaapiDriver.Nouveau:
                    return "nouveau";
            }
        }

        return Option<string>.None;
    }

    private static Option<string> VaapiDeviceName(HardwareAccelerationMode accelerationMode, string vaapiDevice) =>
        accelerationMode == HardwareAccelerationMode.Vaapi ? vaapiDevice : Option<string>.None;

    private static string GetVideoFormat(FFmpegPlaybackSettings playbackSettings) =>
        playbackSettings.VideoFormat switch
        {
            FFmpegProfileVideoFormat.Hevc => VideoFormat.Hevc,
            FFmpegProfileVideoFormat.H264 => VideoFormat.H264,
            FFmpegProfileVideoFormat.Mpeg2Video => VideoFormat.Mpeg2Video,
            FFmpegProfileVideoFormat.Copy => VideoFormat.Copy,
            _ => throw new ArgumentOutOfRangeException($"unexpected video format {playbackSettings.VideoFormat}")
        };
}
