﻿using CliWrap;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.Pipeline;
using ErsatzTV.FFmpeg.Preset;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;
using MediaStream = ErsatzTV.Core.Domain.MediaStream;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegLibraryProcessService : IFFmpegProcessService
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly FFmpegProcessService _ffmpegProcessService;
    private readonly IFFmpegStreamSelector _ffmpegStreamSelector;
    private readonly ILogger<FFmpegLibraryProcessService> _logger;
    private readonly IPipelineBuilderFactory _pipelineBuilderFactory;
    private readonly ITempFilePool _tempFilePool;

    public FFmpegLibraryProcessService(
        FFmpegProcessService ffmpegProcessService,
        IFFmpegStreamSelector ffmpegStreamSelector,
        ITempFilePool tempFilePool,
        IPipelineBuilderFactory pipelineBuilderFactory,
        IConfigElementRepository configElementRepository,
        ILogger<FFmpegLibraryProcessService> logger)
    {
        _ffmpegProcessService = ffmpegProcessService;
        _ffmpegStreamSelector = ffmpegStreamSelector;
        _tempFilePool = tempFilePool;
        _pipelineBuilderFactory = pipelineBuilderFactory;
        _configElementRepository = configElementRepository;
        _logger = logger;
    }

    public async Task<Command> ForPlayoutItem(
        string ffmpegPath,
        string ffprobePath,
        bool saveReports,
        Channel channel,
        MediaVersion videoVersion,
        MediaItemAudioVersion audioVersion,
        string videoPath,
        string audioPath,
        Func<FFmpegPlaybackSettings, Task<List<Subtitle>>> getSubtitles,
        string preferredAudioLanguage,
        string preferredAudioTitle,
        string preferredSubtitleLanguage,
        ChannelSubtitleMode subtitleMode,
        DateTimeOffset start,
        DateTimeOffset finish,
        DateTimeOffset now,
        Option<ChannelWatermark> playoutItemWatermark,
        Option<ChannelWatermark> globalWatermark,
        string vaapiDisplay,
        VaapiDriver vaapiDriver,
        string vaapiDevice,
        Option<int> qsvExtraHardwareFrames,
        bool hlsRealtime,
        FillerKind fillerKind,
        TimeSpan inPoint,
        TimeSpan outPoint,
        long ptsOffset,
        Option<int> targetFramerate,
        bool disableWatermarks,
        Action<FFmpegPipeline> pipelineAction)
    {
        MediaStream videoStream = await _ffmpegStreamSelector.SelectVideoStream(videoVersion);
        Option<MediaStream> maybeAudioStream =
            await _ffmpegStreamSelector.SelectAudioStream(
                audioVersion,
                channel.StreamingMode,
                channel,
                preferredAudioLanguage,
                preferredAudioTitle);

        FFmpegPlaybackSettings playbackSettings = FFmpegPlaybackSettingsCalculator.CalculateSettings(
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

        List<Subtitle> allSubtitles = await getSubtitles(playbackSettings);

        Option<Subtitle> maybeSubtitle =
            await _ffmpegStreamSelector.SelectSubtitleStream(
                allSubtitles,
                channel,
                preferredSubtitleLanguage,
                subtitleMode);

        foreach (Subtitle subtitle in maybeSubtitle)
        {
            if (subtitle.SubtitleKind == SubtitleKind.Sidecar)
            {
                // proxy to avoid dealing with escaping
                subtitle.Path = $"http://localhost:{Settings.ListenPort}/media/subtitle/{subtitle.Id}";
            }
        }

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
            playbackSettings.NormalizeLoudnessMode switch
            {
                NormalizeLoudnessMode.LoudNorm => AudioFilter.LoudNorm,
                _ => AudioFilter.None
            });

        // don't log generated images, or hls direct, which are expected to have unknown format
        bool isUnknownPixelFormatExpected =
            videoPath != audioPath || channel.StreamingMode == StreamingMode.HttpLiveStreamingDirect;
        ILogger<FFmpegLibraryProcessService> pixelFormatLogger = isUnknownPixelFormatExpected ? null : _logger;

        IPixelFormat pixelFormat = await AvailablePixelFormats
            .ForPixelFormat(videoStream.PixelFormat, pixelFormatLogger)
            .IfNoneAsync(
                () =>
                {
                    return videoStream.BitsPerRawSample switch
                    {
                        8 => new PixelFormatYuv420P(),
                        10 => new PixelFormatYuv420P10Le(),
                        _ => new PixelFormatUnknown(videoStream.BitsPerRawSample)
                    };
                });

        var ffmpegVideoStream = new VideoStream(
            videoStream.Index,
            videoStream.Codec,
            videoStream.Profile,
            Some(pixelFormat),
            new ColorParams(
                videoStream.ColorRange,
                videoStream.ColorSpace,
                videoStream.ColorTransfer,
                videoStream.ColorPrimaries),
            new FrameSize(videoVersion.Width, videoVersion.Height),
            videoVersion.SampleAspectRatio,
            videoVersion.DisplayAspectRatio,
            videoVersion.RFrameRate,
            videoPath != audioPath, // still image when paths are different
            videoVersion.VideoScanKind == VideoScanKind.Progressive ? ScanKind.Progressive : ScanKind.Interlaced);

        var videoInputFile = new VideoInputFile(videoPath, new List<VideoStream> { ffmpegVideoStream });

        Option<AudioInputFile> audioInputFile = maybeAudioStream.Map(
            audioStream =>
            {
                var ffmpegAudioStream = new AudioStream(audioStream.Index, audioStream.Codec, audioStream.Channels);
                return new AudioInputFile(audioPath, new List<AudioStream> { ffmpegAudioStream }, audioState);
            });

        // when no audio streams are available, use null audio source
        if (!audioVersion.MediaVersion.Streams.Any(s => s.MediaStreamKind is MediaStreamKind.Audio))
        {
            audioInputFile = new NullAudioInputFile(audioState with { AudioDuration = playbackSettings.AudioDuration });
        }

        OutputFormatKind outputFormat = OutputFormatKind.MpegTs;
        switch (channel.StreamingMode)
        {
            case StreamingMode.HttpLiveStreamingSegmenter:
                outputFormat = OutputFormatKind.Hls;
                break;
            case StreamingMode.HttpLiveStreamingSegmenterV2:
                outputFormat = OutputFormatKind.Nut;
                break;
            case StreamingMode.HttpLiveStreamingDirect:
            {
                // use mpeg-ts by default
                outputFormat = OutputFormatKind.MpegTs;

                // override with setting if applicable
                Option<OutputFormatKind> maybeOutputFormat = await _configElementRepository
                    .GetValue<OutputFormatKind>(ConfigElementKey.FFmpegHlsDirectOutputFormat);
                foreach (OutputFormatKind of in maybeOutputFormat)
                {
                    outputFormat = of;
                }

                break;
            }
        }

        Option<string> subtitleLanguage = Option<string>.None;
        Option<string> subtitleTitle = Option<string>.None;

        Option<SubtitleInputFile> subtitleInputFile = maybeSubtitle.Map<Option<SubtitleInputFile>>(
            subtitle =>
            {
                if (!subtitle.IsImage && subtitle.SubtitleKind == SubtitleKind.Embedded &&
                    (!subtitle.IsExtracted || string.IsNullOrWhiteSpace(subtitle.Path)))
                {
                    _logger.LogWarning("Subtitles are not yet available for this item");
                    return None;
                }

                var ffmpegSubtitleStream = new ErsatzTV.FFmpeg.MediaStream(
                    subtitle.IsImage ? subtitle.StreamIndex : 0,
                    subtitle.Codec,
                    StreamKind.Video);

                string path = subtitle.IsImage switch
                {
                    true => videoPath,
                    false when subtitle.SubtitleKind == SubtitleKind.Sidecar => subtitle.Path,
                    _ => Path.Combine(FileSystemLayout.SubtitleCacheFolder, subtitle.Path)
                };

                SubtitleMethod method = SubtitleMethod.Burn;
                if (channel.StreamingMode == StreamingMode.HttpLiveStreamingDirect)
                {
                    method = (outputFormat, subtitle.SubtitleKind, subtitle.Codec) switch
                    {
                        // mkv supports all subtitle codecs, maybe?
                        (OutputFormatKind.Mkv, SubtitleKind.Embedded, _) => SubtitleMethod.Copy,

                        // MP4 supports vobsub
                        (OutputFormatKind.Mp4, SubtitleKind.Embedded, "dvdsub" or "dvd_subtitle" or "vobsub") =>
                            SubtitleMethod.Copy,

                        // MP4 does not support PGS
                        (OutputFormatKind.Mp4, SubtitleKind.Embedded, "pgs" or "pgssub" or "hdmv_pgs_subtitle") =>
                            SubtitleMethod.None,

                        // ignore text subtitles for now
                        _ => SubtitleMethod.None
                    };

                    if (method == SubtitleMethod.None)
                    {
                        return None;
                    }

                    // hls direct won't use extracted embedded subtitles
                    if (subtitle.SubtitleKind == SubtitleKind.Embedded)
                    {
                        path = videoPath;
                        ffmpegSubtitleStream = ffmpegSubtitleStream with { Index = subtitle.StreamIndex };
                    }
                }

                if (method == SubtitleMethod.Copy)
                {
                    subtitleLanguage = Optional(subtitle.Language);
                    subtitleTitle = Optional(subtitle.Title);
                }

                return new SubtitleInputFile(
                    path,
                    new List<ErsatzTV.FFmpeg.MediaStream> { ffmpegSubtitleStream },
                    method);
            }).Flatten();

        Option<WatermarkInputFile> watermarkInputFile = GetWatermarkInputFile(watermarkOptions, maybeFadePoints);

        HardwareAccelerationMode hwAccel = GetHardwareAccelerationMode(playbackSettings, fillerKind);

        string videoFormat = GetVideoFormat(playbackSettings);
        Option<string> maybeVideoProfile = GetVideoProfile(videoFormat, channel.FFmpegProfile.VideoProfile);
        Option<string> maybeVideoPreset = GetVideoPreset(hwAccel, videoFormat, channel.FFmpegProfile.VideoPreset);

        Option<string> hlsPlaylistPath = outputFormat == OutputFormatKind.Hls
            ? Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live.m3u8")
            : Option<string>.None;

        Option<string> hlsSegmentTemplate = outputFormat == OutputFormatKind.Hls
            ? Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live%06d.ts")
            : Option<string>.None;

        FrameSize scaledSize = ffmpegVideoStream.SquarePixelFrameSize(
            new FrameSize(channel.FFmpegProfile.Resolution.Width, channel.FFmpegProfile.Resolution.Height));

        var paddedSize = new FrameSize(
            channel.FFmpegProfile.Resolution.Width,
            channel.FFmpegProfile.Resolution.Height);

        Option<FrameSize> cropSize = Option<FrameSize>.None;

        if (channel.FFmpegProfile.ScalingBehavior is ScalingBehavior.Stretch)
        {
            scaledSize = paddedSize;
        }

        if (channel.FFmpegProfile.ScalingBehavior is ScalingBehavior.Crop)
        {
            bool isTooSmallToCrop = videoVersion.Height < channel.FFmpegProfile.Resolution.Height ||
                                    videoVersion.Width < channel.FFmpegProfile.Resolution.Width;

            // if any dimension is smaller than the crop, scale beyond the crop (beyond the target resolution)
            if (isTooSmallToCrop)
            {
                foreach (IDisplaySize size in playbackSettings.ScaledSize)
                {
                    scaledSize = new FrameSize(size.Width, size.Height);
                }

                paddedSize = scaledSize;
            }
            else
            {
                paddedSize = ffmpegVideoStream.SquarePixelFrameSizeForCrop(
                    new FrameSize(channel.FFmpegProfile.Resolution.Width, channel.FFmpegProfile.Resolution.Height));
            }

            cropSize = new FrameSize(
                channel.FFmpegProfile.Resolution.Width,
                channel.FFmpegProfile.Resolution.Height);
        }

        var desiredState = new FrameState(
            playbackSettings.RealtimeOutput,
            fillerKind == FillerKind.Fallback,
            videoFormat,
            maybeVideoProfile,
            maybeVideoPreset,
            channel.FFmpegProfile.AllowBFrames,
            Optional(playbackSettings.PixelFormat),
            scaledSize,
            paddedSize,
            cropSize,
            false,
            playbackSettings.FrameRate,
            playbackSettings.VideoBitrate,
            playbackSettings.VideoBufferSize,
            playbackSettings.VideoTrackTimeScale,
            playbackSettings.Deinterlace);

        var ffmpegState = new FFmpegState(
            saveReports,
            hwAccel,
            hwAccel,
            VaapiDriverName(hwAccel, vaapiDriver),
            VaapiDeviceName(hwAccel, vaapiDevice),
            playbackSettings.StreamSeek,
            finish - now,
            channel.StreamingMode != StreamingMode.HttpLiveStreamingDirect,
            "ErsatzTV",
            channel.Name,
            maybeAudioStream.Map(s => Optional(s.Language)).Flatten(),
            subtitleLanguage,
            subtitleTitle,
            outputFormat,
            hlsPlaylistPath,
            hlsSegmentTemplate,
            ptsOffset,
            playbackSettings.ThreadCount,
            qsvExtraHardwareFrames,
            videoVersion is BackgroundImageMediaVersion { IsSongWithProgress: true });

        _logger.LogDebug("FFmpeg desired state {FrameState}", desiredState);

        IPipelineBuilder pipelineBuilder = await _pipelineBuilderFactory.GetBuilder(
            hwAccel,
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            subtitleInputFile,
            Option<ConcatInputFile>.None,
            VaapiDisplayName(hwAccel, vaapiDisplay),
            VaapiDriverName(hwAccel, vaapiDriver),
            VaapiDeviceName(hwAccel, vaapiDevice),
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.Build(ffmpegState, desiredState);

        pipelineAction?.Invoke(pipeline);

        return GetCommand(ffmpegPath, videoInputFile, audioInputFile, watermarkInputFile, None, pipeline);
    }

    public async Task<Command> ForError(
        string ffmpegPath,
        Channel channel,
        Option<TimeSpan> duration,
        string errorMessage,
        bool hlsRealtime,
        long ptsOffset,
        string vaapiDisplay,
        VaapiDriver vaapiDriver,
        string vaapiDevice,
        Option<int> qsvExtraHardwareFrames)
    {
        FFmpegPlaybackSettings playbackSettings = FFmpegPlaybackSettingsCalculator.CalculateErrorSettings(
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
            AudioFilter.None);

        string videoFormat = GetVideoFormat(playbackSettings);

        var desiredState = new FrameState(
            playbackSettings.RealtimeOutput,
            false,
            videoFormat,
            GetVideoProfile(videoFormat, channel.FFmpegProfile.VideoProfile),
            VideoPreset.Unset,
            channel.FFmpegProfile.AllowBFrames,
            new PixelFormatYuv420P(),
            new FrameSize(desiredResolution.Width, desiredResolution.Height),
            new FrameSize(desiredResolution.Width, desiredResolution.Height),
            Option<FrameSize>.None,
            false,
            playbackSettings.FrameRate,
            playbackSettings.VideoBitrate,
            playbackSettings.VideoBufferSize,
            playbackSettings.VideoTrackTimeScale,
            playbackSettings.Deinterlace);

        OutputFormatKind outputFormat = OutputFormatKind.MpegTs;
        switch (channel.StreamingMode)
        {
            case StreamingMode.HttpLiveStreamingSegmenter:
                outputFormat = OutputFormatKind.Hls;
                break;
            case StreamingMode.HttpLiveStreamingSegmenterV2:
                outputFormat = OutputFormatKind.Nut;
                break;
        }

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
            string.Empty,
            new PixelFormatUnknown(), // leave this unknown so we convert to desired yuv420p
            ColorParams.Default,
            new FrameSize(videoVersion.Width, videoVersion.Height),
            videoVersion.SampleAspectRatio,
            videoVersion.DisplayAspectRatio,
            None,
            true,
            ScanKind.Progressive);

        var videoInputFile = new VideoInputFile(videoPath, new List<VideoStream> { ffmpegVideoStream });

        // TODO: ignore accel if this already failed once
        HardwareAccelerationMode hwAccel = GetHardwareAccelerationMode(playbackSettings, FillerKind.None);
        _logger.LogDebug("HW accel mode: {HwAccel}", hwAccel);

        var ffmpegState = new FFmpegState(
            false,
            HardwareAccelerationMode.None, // no hw accel decode since errors loop
            hwAccel,
            VaapiDriverName(hwAccel, vaapiDriver),
            VaapiDeviceName(hwAccel, vaapiDevice),
            playbackSettings.StreamSeek,
            duration,
            channel.StreamingMode != StreamingMode.HttpLiveStreamingDirect,
            "ErsatzTV",
            channel.Name,
            None,
            None,
            None,
            outputFormat,
            hlsPlaylistPath,
            hlsSegmentTemplate,
            ptsOffset,
            Option<int>.None,
            qsvExtraHardwareFrames,
            IsSongWithProgress: false);

        var ffmpegSubtitleStream = new ErsatzTV.FFmpeg.MediaStream(0, "ass", StreamKind.Video);

        var audioInputFile = new NullAudioInputFile(audioState);

        var subtitleInputFile = new SubtitleInputFile(
            subtitleFile,
            new List<ErsatzTV.FFmpeg.MediaStream> { ffmpegSubtitleStream },
            SubtitleMethod.Burn);

        _logger.LogDebug("FFmpeg desired error state {FrameState}", desiredState);

        IPipelineBuilder pipelineBuilder = await _pipelineBuilderFactory.GetBuilder(
            hwAccel,
            videoInputFile,
            audioInputFile,
            None,
            subtitleInputFile,
            Option<ConcatInputFile>.None,
            VaapiDisplayName(hwAccel, vaapiDisplay),
            VaapiDriverName(hwAccel, vaapiDriver),
            VaapiDeviceName(hwAccel, vaapiDevice),
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.Build(ffmpegState, desiredState);

        return GetCommand(ffmpegPath, videoInputFile, audioInputFile, None, None, pipeline);
    }

    public async Task<Command> ConcatChannel(
        string ffmpegPath,
        bool saveReports,
        Channel channel,
        string scheme,
        string host)
    {
        var resolution = new FrameSize(channel.FFmpegProfile.Resolution.Width, channel.FFmpegProfile.Resolution.Height);

        var concatInputFile = new ConcatInputFile(
            $"http://localhost:{Settings.ListenPort}/ffmpeg/concat/{channel.Number}?mode=ts-legacy",
            resolution);

        IPipelineBuilder pipelineBuilder = await _pipelineBuilderFactory.GetBuilder(
            HardwareAccelerationMode.None,
            None,
            None,
            None,
            None,
            concatInputFile,
            Option<string>.None,
            None,
            None,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.Concat(
            concatInputFile,
            FFmpegState.Concat(saveReports, channel.Name));

        return GetCommand(ffmpegPath, None, None, None, concatInputFile, pipeline);
    }

    public async Task<Command> ConcatSegmenterChannel(
        string ffmpegPath,
        bool saveReports,
        Channel channel,
        string scheme,
        string host)
    {
        var resolution = new FrameSize(channel.FFmpegProfile.Resolution.Width, channel.FFmpegProfile.Resolution.Height);
        var concatInputFile = new ConcatInputFile(
            $"http://localhost:{Settings.ListenPort}/ffmpeg/concat/{channel.Number}?mode=segmenter-v2",
            resolution);

        FFmpegPlaybackSettings playbackSettings = FFmpegPlaybackSettingsCalculator.CalculateConcatSegmenterSettings(
            channel.FFmpegProfile,
            Option<int>.None);

        playbackSettings.AudioDuration = Option<TimeSpan>.None;

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
            Option<TimeSpan>.None,
            playbackSettings.NormalizeLoudnessMode switch
            {
                // TODO: NormalizeLoudnessMode.LoudNorm => AudioFilter.LoudNorm,
                _ => AudioFilter.None
            });

        IPixelFormat pixelFormat = channel.FFmpegProfile.BitDepth switch
        {
            FFmpegProfileBitDepth.TenBit => new PixelFormatYuv420P10Le(),
            _ => new PixelFormatYuv420P()
        };

        var ffmpegVideoStream = new VideoStream(
            0,
            VideoFormat.Raw,
            string.Empty,
            Some(pixelFormat),
            ColorParams.Default,
            resolution,
            "1:1",
            string.Empty,
            Option<string>.None,
            false,
            ScanKind.Progressive);

        var videoInputFile = new VideoInputFile(concatInputFile.Url, new List<VideoStream> { ffmpegVideoStream });

        var ffmpegAudioStream = new AudioStream(1, string.Empty, channel.FFmpegProfile.AudioChannels);
        Option<AudioInputFile> audioInputFile = new AudioInputFile(
            concatInputFile.Url,
            new List<AudioStream> { ffmpegAudioStream },
            audioState);

        Option<SubtitleInputFile> subtitleInputFile = Option<SubtitleInputFile>.None;
        Option<WatermarkInputFile> watermarkInputFile = Option<WatermarkInputFile>.None;

        HardwareAccelerationMode hwAccel = GetHardwareAccelerationMode(playbackSettings, FillerKind.None);

        string videoFormat = GetVideoFormat(playbackSettings);
        Option<string> maybeVideoProfile = GetVideoProfile(videoFormat, channel.FFmpegProfile.VideoProfile);
        Option<string> maybeVideoPreset = GetVideoPreset(hwAccel, videoFormat, channel.FFmpegProfile.VideoPreset);

        Option<string> hlsPlaylistPath = Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live.m3u8");

        Option<string> hlsSegmentTemplate = videoFormat switch
        {
            // hls/hevc needs mp4
            VideoFormat.Hevc => Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live%06d.m4s"),

            // hls is otherwise fine with ts
            _ => Path.Combine(FileSystemLayout.TranscodeFolder, channel.Number, "live%06d.ts")
        };

        var desiredState = new FrameState(
            playbackSettings.RealtimeOutput,
            true,
            videoFormat,
            maybeVideoProfile,
            maybeVideoPreset,
            channel.FFmpegProfile.AllowBFrames,
            Optional(playbackSettings.PixelFormat),
            resolution,
            resolution,
            Option<FrameSize>.None,
            false,
            playbackSettings.FrameRate,
            playbackSettings.VideoBitrate,
            playbackSettings.VideoBufferSize,
            playbackSettings.VideoTrackTimeScale,
            playbackSettings.Deinterlace);

        Option<string> vaapiDisplay = VaapiDisplayName(hwAccel, channel.FFmpegProfile.VaapiDisplay);
        Option<string> vaapiDriver = VaapiDriverName(hwAccel, channel.FFmpegProfile.VaapiDriver);
        Option<string> vaapiDevice = VaapiDeviceName(hwAccel, channel.FFmpegProfile.VaapiDevice);

        var ffmpegState = new FFmpegState(
            saveReports,
            HardwareAccelerationMode.None,
            hwAccel,
            vaapiDriver,
            vaapiDevice,
            playbackSettings.StreamSeek,
            Option<TimeSpan>.None,
            channel.StreamingMode != StreamingMode.HttpLiveStreamingDirect,
            "ErsatzTV",
            channel.Name,
            Option<string>.None,
            Option<string>.None,
            Option<string>.None,
            OutputFormatKind.Hls,
            hlsPlaylistPath,
            hlsSegmentTemplate,
            0,
            playbackSettings.ThreadCount,
            Optional(channel.FFmpegProfile.QsvExtraHardwareFrames),
            IsSongWithProgress: false);

        _logger.LogDebug("FFmpeg desired state {FrameState}", desiredState);

        IPipelineBuilder pipelineBuilder = await _pipelineBuilderFactory.GetBuilder(
            hwAccel,
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            subtitleInputFile,
            concatInputFile,
            vaapiDisplay,
            vaapiDriver,
            vaapiDevice,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.Build(ffmpegState, desiredState);

        // copy video input options to concat input
        concatInputFile.InputOptions.AddRange(videoInputFile.InputOptions);

        return GetCommand(ffmpegPath, None, None, None, concatInputFile, pipeline);
    }

    public async Task<Command> WrapSegmenter(
        string ffmpegPath,
        bool saveReports,
        Channel channel,
        string scheme,
        string host,
        string accessToken)
    {
        var resolution = new FrameSize(channel.FFmpegProfile.Resolution.Width, channel.FFmpegProfile.Resolution.Height);

        string accessTokenQuery = string.IsNullOrWhiteSpace(accessToken)
            ? string.Empty
            : $"&access_token={accessToken}";

        var concatInputFile = new ConcatInputFile(
            $"http://localhost:{Settings.ListenPort}/iptv/channel/{channel.Number}.m3u8?mode=segmenter{accessTokenQuery}",
            resolution);

        IPipelineBuilder pipelineBuilder = await _pipelineBuilderFactory.GetBuilder(
            HardwareAccelerationMode.None,
            None,
            None,
            None,
            None,
            concatInputFile,
            Option<string>.None,
            None,
            None,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.WrapSegmenter(
            concatInputFile,
            FFmpegState.Concat(saveReports, channel.Name));

        return GetCommand(ffmpegPath, None, None, None, concatInputFile, pipeline);
    }

    public async Task<Command> ResizeImage(string ffmpegPath, string inputFile, string outputFile, int height)
    {
        var videoInputFile = new VideoInputFile(
            inputFile,
            new List<VideoStream>
            {
                new(
                    0,
                    string.Empty,
                    string.Empty,
                    None,
                    ColorParams.Default,
                    FrameSize.Unknown,
                    string.Empty,
                    string.Empty,
                    None,
                    true,
                    ScanKind.Progressive)
            });

        IPipelineBuilder pipelineBuilder = await _pipelineBuilderFactory.GetBuilder(
            HardwareAccelerationMode.None,
            videoInputFile,
            None,
            None,
            None,
            Option<ConcatInputFile>.None,
            Option<string>.None,
            None,
            None,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.Resize(outputFile, new FrameSize(-1, height));

        return GetCommand(ffmpegPath, videoInputFile, None, None, None, pipeline, false);
    }

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

    private static Option<WatermarkInputFile> GetWatermarkInputFile(
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
                                    string.Empty,
                                    new PixelFormatUnknown(),
                                    ColorParams.Default,
                                    new FrameSize(1, 1),
                                    string.Empty,
                                    string.Empty,
                                    Option<string>.None,
                                    !options.IsAnimated,
                                    ScanKind.Progressive)
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
                                watermark.Opacity,
                                watermark.PlaceWithinSourceContent));

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
        IEnumerable<string> loggedAudioFilters =
            audioInputFile.Map(f => f.FilterSteps.Map(af => af.GetType().Name)).Flatten();
        IEnumerable<string> loggedVideoFilters =
            videoInputFile.Map(f => f.FilterSteps.Map(vf => vf.GetType().Name)).Flatten();

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
            pipeline.PipelineSteps,
            pipeline.IsIntelVaapiOrQsv);

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

    private static Option<string> VaapiDisplayName(HardwareAccelerationMode accelerationMode, string vaapiDisplay) =>
        accelerationMode == HardwareAccelerationMode.Vaapi ? vaapiDisplay : Option<string>.None;

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
        accelerationMode == HardwareAccelerationMode.Vaapi ||
        OperatingSystem.IsLinux() && accelerationMode == HardwareAccelerationMode.Qsv
            ? string.IsNullOrWhiteSpace(vaapiDevice) ? "/dev/dri/renderD128" : vaapiDevice
            : Option<string>.None;

    private static string GetVideoFormat(FFmpegPlaybackSettings playbackSettings) =>
        playbackSettings.VideoFormat switch
        {
            FFmpegProfileVideoFormat.Hevc => VideoFormat.Hevc,
            FFmpegProfileVideoFormat.H264 => VideoFormat.H264,
            FFmpegProfileVideoFormat.Mpeg2Video => VideoFormat.Mpeg2Video,
            FFmpegProfileVideoFormat.Copy => VideoFormat.Copy,
            _ => throw new ArgumentOutOfRangeException($"unexpected video format {playbackSettings.VideoFormat}")
        };

    private static Option<string> GetVideoProfile(string videoFormat, string videoProfile) =>
        (videoFormat, (videoProfile ?? string.Empty).ToLowerInvariant()) switch
        {
            (VideoFormat.H264, VideoProfile.Main) => VideoProfile.Main,
            (VideoFormat.H264, VideoProfile.High) => VideoProfile.High,
            _ => Option<string>.None
        };

    private static Option<string> GetVideoPreset(
        HardwareAccelerationMode hardwareAccelerationMode,
        string videoFormat,
        string videoPreset) =>
        AvailablePresets
            .ForAccelAndFormat(hardwareAccelerationMode, videoFormat)
            .Find(p => string.Equals(p, videoPreset, StringComparison.OrdinalIgnoreCase));

    private static HardwareAccelerationMode GetHardwareAccelerationMode(
        FFmpegPlaybackSettings playbackSettings,
        FillerKind fillerKind) =>
        playbackSettings.HardwareAcceleration switch
        {
            _ when fillerKind == FillerKind.Fallback => HardwareAccelerationMode.None,
            HardwareAccelerationKind.Nvenc => HardwareAccelerationMode.Nvenc,
            HardwareAccelerationKind.Qsv => HardwareAccelerationMode.Qsv,
            HardwareAccelerationKind.Vaapi => HardwareAccelerationMode.Vaapi,
            HardwareAccelerationKind.VideoToolbox => HardwareAccelerationMode.VideoToolbox,
            HardwareAccelerationKind.Amf => HardwareAccelerationMode.Amf,
            _ => HardwareAccelerationMode.None
        };
}
