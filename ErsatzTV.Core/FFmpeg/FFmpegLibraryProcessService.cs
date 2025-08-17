using System.Collections.Immutable;
using CliWrap;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Graphics;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.Pipeline;
using ErsatzTV.FFmpeg.Preset;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MediaStream = ErsatzTV.Core.Domain.MediaStream;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegLibraryProcessService : IFFmpegProcessService
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly ICustomStreamSelector _customStreamSelector;
    private readonly IWatermarkSelector _watermarkSelector;
    private readonly FFmpegProcessService _ffmpegProcessService;
    private readonly IFFmpegStreamSelector _ffmpegStreamSelector;
    private readonly ILogger<FFmpegLibraryProcessService> _logger;
    private readonly IPipelineBuilderFactory _pipelineBuilderFactory;
    private readonly ITempFilePool _tempFilePool;

    public FFmpegLibraryProcessService(
        FFmpegProcessService ffmpegProcessService,
        IFFmpegStreamSelector ffmpegStreamSelector,
        ICustomStreamSelector customStreamSelector,
        IWatermarkSelector watermarkSelector,
        ITempFilePool tempFilePool,
        IPipelineBuilderFactory pipelineBuilderFactory,
        IConfigElementRepository configElementRepository,
        ILogger<FFmpegLibraryProcessService> logger)
    {
        _ffmpegProcessService = ffmpegProcessService;
        _ffmpegStreamSelector = ffmpegStreamSelector;
        _customStreamSelector = customStreamSelector;
        _watermarkSelector = watermarkSelector;
        _tempFilePool = tempFilePool;
        _pipelineBuilderFactory = pipelineBuilderFactory;
        _configElementRepository = configElementRepository;
        _logger = logger;
    }

    public async Task<PlayoutItemResult> ForPlayoutItem(
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
        List<ChannelWatermark> playoutItemWatermarks,
        Option<ChannelWatermark> globalWatermark,
        List<PlayoutItemGraphicsElement> graphicsElements,
        string vaapiDisplay,
        VaapiDriver vaapiDriver,
        string vaapiDevice,
        Option<int> qsvExtraHardwareFrames,
        bool hlsRealtime,
        StreamInputKind streamInputKind,
        FillerKind fillerKind,
        TimeSpan inPoint,
        TimeSpan outPoint,
        DateTimeOffset channelStartTime,
        long ptsOffset,
        Option<int> targetFramerate,
        bool disableWatermarks,
        Option<string> customReportsFolder,
        Action<FFmpegPipeline> pipelineAction)
    {
        MediaStream videoStream = await _ffmpegStreamSelector.SelectVideoStream(videoVersion);

        // we cannot burst live input
        hlsRealtime = hlsRealtime || streamInputKind is StreamInputKind.Live;

        FFmpegPlaybackSettings playbackSettings = FFmpegPlaybackSettingsCalculator.CalculateSettings(
            channel.StreamingMode,
            channel.FFmpegProfile,
            videoVersion,
            videoStream,
            start,
            now,
            inPoint,
            outPoint,
            hlsRealtime,
            streamInputKind,
            targetFramerate);

        List<Subtitle> allSubtitles = await getSubtitles(playbackSettings);

        Option<MediaStream> maybeAudioStream = Option<MediaStream>.None;
        Option<Subtitle> maybeSubtitle = Option<Subtitle>.None;

        if (channel.StreamSelectorMode is ChannelStreamSelectorMode.Custom)
        {
            StreamSelectorResult result = await _customStreamSelector.SelectStreams(
                channel,
                start,
                audioVersion,
                allSubtitles);
            maybeAudioStream = result.AudioStream;
            maybeSubtitle = result.Subtitle;

            if (maybeAudioStream.IsNone)
            {
                _logger.LogWarning(
                    "No audio stream found using custom stream selector {StreamSelector}; will use default stream selection logic",
                    channel.StreamSelector);
            }
        }

        if (channel.StreamSelectorMode is ChannelStreamSelectorMode.Default || maybeAudioStream.IsNone)
        {
            maybeAudioStream =
                await _ffmpegStreamSelector.SelectAudioStream(
                    audioVersion,
                    channel.StreamingMode,
                    channel,
                    preferredAudioLanguage,
                    preferredAudioTitle);

            maybeSubtitle =
                await _ffmpegStreamSelector.SelectSubtitleStream(
                    allSubtitles.ToImmutableList(),
                    channel,
                    preferredSubtitleLanguage,
                    subtitleMode);
        }

        if (channel.StreamSelectorMode is ChannelStreamSelectorMode.Troubleshooting && maybeSubtitle.IsNone)
        {
            maybeSubtitle = allSubtitles.HeadOrNone();
        }

        foreach (Subtitle subtitle in maybeSubtitle)
        {
            if (subtitle.SubtitleKind == SubtitleKind.Sidecar || subtitle is
                    { SubtitleKind: SubtitleKind.Embedded, IsImage: false, IsExtracted: true })
            {
                // proxy to avoid dealing with escaping
                subtitle.Path = $"http://localhost:{Settings.StreamingPort}/media/subtitle/{subtitle.Id}";

                foreach (TimeSpan seek in playbackSettings.StreamSeek)
                {
                    subtitle.Path += $"?seekToMs={(int)seek.TotalMilliseconds}";
                }
            }
        }

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
            .IfNoneAsync(() =>
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

        var videoInputFile = new VideoInputFile(
            videoPath,
            new List<VideoStream> { ffmpegVideoStream },
            streamInputKind);

        Option<AudioInputFile> audioInputFile = maybeAudioStream.Map(audioStream =>
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

        Option<SubtitleInputFile> subtitleInputFile = maybeSubtitle.Map<Option<SubtitleInputFile>>(subtitle =>
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

            string path = subtitle.IsImage ? videoPath : subtitle.Path;

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

        Option<WatermarkInputFile> watermarkInputFile = Option<WatermarkInputFile>.None;
        Option<GraphicsEngineInput> graphicsEngineInput = Option<GraphicsEngineInput>.None;
        Option<GraphicsEngineContext> graphicsEngineContext = Option<GraphicsEngineContext>.None;
        List<GraphicsElementContext> graphicsElementContexts = [];

        // use graphics engine for all watermarks
        if (!disableWatermarks)
        {
            var watermarks = new Dictionary<int, WatermarkElementContext>();

            // still need channel and global watermarks
            if (playoutItemWatermarks.Count == 0)
            {
                WatermarkOptions options = await _watermarkSelector.GetWatermarkOptions(
                    channel,
                    Option<ChannelWatermark>.None,
                    globalWatermark,
                    videoVersion,
                    None,
                    None);

                foreach (ChannelWatermark watermark in options.Watermark)
                {
                    // don't allow duplicates
                    watermarks.TryAdd(watermark.Id, new WatermarkElementContext(options));
                }
            }

            // load all playout item watermarks
            foreach (ChannelWatermark playoutItemWatermark in playoutItemWatermarks)
            {
                WatermarkOptions options = await _watermarkSelector.GetWatermarkOptions(
                    channel,
                    playoutItemWatermark,
                    globalWatermark,
                    videoVersion,
                    None,
                    None);

                foreach (ChannelWatermark watermark in options.Watermark)
                {
                    // don't allow duplicates
                    watermarks.TryAdd(watermark.Id, new WatermarkElementContext(options));
                }
            }

            graphicsElementContexts.AddRange(watermarks.Values);
        }

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

        foreach (PlayoutItemGraphicsElement playoutItemGraphicsElement in graphicsElements)
        {
            switch (playoutItemGraphicsElement.GraphicsElement.Kind)
            {
                case GraphicsElementKind.Text:
                {
                    Option<TextGraphicsElement> maybeElement =
                        await TextGraphicsElement.FromFile(playoutItemGraphicsElement.GraphicsElement.Path);
                    if (maybeElement.IsNone)
                    {
                        _logger.LogWarning(
                            "Failed to load text graphics element from file {Path}; ignoring",
                            playoutItemGraphicsElement.GraphicsElement.Path);
                    }

                    foreach (TextGraphicsElement element in maybeElement)
                    {
                        var variables = new Dictionary<string, string>();
                        if (!string.IsNullOrWhiteSpace(playoutItemGraphicsElement.Variables))
                        {
                            variables = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                                playoutItemGraphicsElement.Variables);
                        }

                        graphicsElementContexts.Add(new TextElementDataContext(element, variables));
                    }

                    break;
                }
                case GraphicsElementKind.Image:
                {
                    Option<ImageGraphicsElement> maybeElement =
                        await ImageGraphicsElement.FromFile(playoutItemGraphicsElement.GraphicsElement.Path);
                    if (maybeElement.IsNone)
                    {
                        _logger.LogWarning(
                            "Failed to load image graphics element from file {Path}; ignoring",
                            playoutItemGraphicsElement.GraphicsElement.Path);
                    }

                    foreach (ImageGraphicsElement element in maybeElement)
                    {
                        graphicsElementContexts.Add(new ImageElementContext(element));
                    }

                    break;
                }
                case GraphicsElementKind.Subtitle:
                {
                    Option<SubtitlesGraphicsElement> maybeElement =
                        await SubtitlesGraphicsElement.FromFile(playoutItemGraphicsElement.GraphicsElement.Path);
                    if (maybeElement.IsNone)
                    {
                        _logger.LogWarning(
                            "Failed to load subtitle graphics element from file {Path}; ignoring",
                            playoutItemGraphicsElement.GraphicsElement.Path);
                    }

                    foreach (SubtitlesGraphicsElement element in maybeElement)
                    {
                        var variables = new Dictionary<string, string>();
                        if (!string.IsNullOrWhiteSpace(playoutItemGraphicsElement.Variables))
                        {
                            variables = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                                playoutItemGraphicsElement.Variables);
                        }

                        graphicsElementContexts.Add(new SubtitleElementDataContext(element, variables));
                    }

                    break;
                }
                default:
                    _logger.LogInformation(
                        "Ignoring unsupported graphics element kind {Kind}",
                        nameof(playoutItemGraphicsElement.GraphicsElement.Kind));
                    break;
            }
        }

        // only use graphics engine when we have elements
        if (graphicsElementContexts.Count > 0)
        {
            graphicsEngineInput = new GraphicsEngineInput();

            graphicsEngineContext = new GraphicsEngineContext(
                channel.Number,
                audioVersion.MediaItem,
                graphicsElementContexts,
                new Resolution { Width = desiredState.ScaledSize.Width, Height = desiredState.ScaledSize.Height },
                channel.FFmpegProfile.Resolution,
                await playbackSettings.FrameRate.IfNoneAsync(24),
                channelStartTime,
                start,
                await playbackSettings.StreamSeek.IfNoneAsync(TimeSpan.Zero),
                finish - now);
        }

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
            videoVersion is BackgroundImageMediaVersion { IsSongWithProgress: true },
            false,
            GetTonemapAlgorithm(playbackSettings),
            channel.UniqueId == Guid.Empty);

        _logger.LogDebug("FFmpeg desired state {FrameState}", desiredState);

        IPipelineBuilder pipelineBuilder = await _pipelineBuilderFactory.GetBuilder(
            hwAccel,
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            subtitleInputFile,
            Option<ConcatInputFile>.None,
            graphicsEngineInput,
            VaapiDisplayName(hwAccel, vaapiDisplay),
            VaapiDriverName(hwAccel, vaapiDriver),
            VaapiDeviceName(hwAccel, vaapiDevice),
            await customReportsFolder.IfNoneAsync(FileSystemLayout.FFmpegReportsFolder),
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.Build(ffmpegState, desiredState);

        pipelineAction?.Invoke(pipeline);

        Command command = GetCommand(
            ffmpegPath,
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            Option<ConcatInputFile>.None,
            graphicsEngineInput,
            pipeline);

        return new PlayoutItemResult(command, graphicsEngineContext);
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

        Resolution desiredResolution = channel.FFmpegProfile.Resolution;

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
            false,
            false,
            GetTonemapAlgorithm(playbackSettings),
            channel.UniqueId == Guid.Empty);

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
            Option<WatermarkInputFile>.None,
            subtitleInputFile,
            Option<ConcatInputFile>.None,
            Option<GraphicsEngineInput>.None,
            VaapiDisplayName(hwAccel, vaapiDisplay),
            VaapiDriverName(hwAccel, vaapiDriver),
            VaapiDeviceName(hwAccel, vaapiDevice),
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.Build(ffmpegState, desiredState);

        return GetCommand(ffmpegPath, videoInputFile, audioInputFile, None, None, None, pipeline);
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
            $"http://localhost:{Settings.StreamingPort}/ffmpeg/concat/{channel.Number}?mode=ts-legacy",
            resolution);

        IPipelineBuilder pipelineBuilder = await _pipelineBuilderFactory.GetBuilder(
            HardwareAccelerationMode.None,
            Option<VideoInputFile>.None,
            Option<AudioInputFile>.None,
            Option<WatermarkInputFile>.None,
            Option<SubtitleInputFile>.None,
            concatInputFile,
            Option<GraphicsEngineInput>.None,
            Option<string>.None,
            Option<string>.None,
            Option<string>.None,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.Concat(
            concatInputFile,
            FFmpegState.Concat(saveReports, channel.Name));

        return GetCommand(ffmpegPath, None, None, None, concatInputFile, None, pipeline);
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
            $"http://localhost:{Settings.StreamingPort}/ffmpeg/concat/{channel.Number}?mode=segmenter-v2",
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
        Option<GraphicsEngineInput> graphicsEngineInput = Option<GraphicsEngineInput>.None;

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
            false,
            false,
            GetTonemapAlgorithm(playbackSettings),
            channel.UniqueId == Guid.Empty);

        _logger.LogDebug("FFmpeg desired state {FrameState}", desiredState);

        IPipelineBuilder pipelineBuilder = await _pipelineBuilderFactory.GetBuilder(
            hwAccel,
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            subtitleInputFile,
            concatInputFile,
            graphicsEngineInput,
            vaapiDisplay,
            vaapiDriver,
            vaapiDevice,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.Build(ffmpegState, desiredState);

        // copy video input options to concat input
        concatInputFile.InputOptions.AddRange(videoInputFile.InputOptions);

        return GetCommand(ffmpegPath, None, None, None, concatInputFile, None, pipeline);
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
            $"http://localhost:{Settings.StreamingPort}/iptv/channel/{channel.Number}.m3u8?mode=segmenter{accessTokenQuery}",
            resolution);

        IPipelineBuilder pipelineBuilder = await _pipelineBuilderFactory.GetBuilder(
            HardwareAccelerationMode.None,
            Option<VideoInputFile>.None,
            Option<AudioInputFile>.None,
            Option<WatermarkInputFile>.None,
            Option<SubtitleInputFile>.None,
            concatInputFile,
            Option<GraphicsEngineInput>.None,
            Option<string>.None,
            Option<string>.None,
            Option<string>.None,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.WrapSegmenter(
            concatInputFile,
            FFmpegState.Concat(saveReports, channel.Name));

        return GetCommand(ffmpegPath, None, None, None, concatInputFile, None, pipeline);
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
            Option<AudioInputFile>.None,
            Option<WatermarkInputFile>.None,
            Option<SubtitleInputFile>.None,
            Option<ConcatInputFile>.None,
            Option<GraphicsEngineInput>.None,
            Option<string>.None,
            Option<string>.None,
            Option<string>.None,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.Resize(outputFile, new FrameSize(-1, height));

        return GetCommand(ffmpegPath, videoInputFile, None, None, None, None, pipeline, false);
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

    public async Task<Command> SeekTextSubtitle(string ffmpegPath, string inputFile, TimeSpan seek)
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
            Option<AudioInputFile>.None,
            Option<WatermarkInputFile>.None,
            Option<SubtitleInputFile>.None,
            Option<ConcatInputFile>.None,
            Option<GraphicsEngineInput>.None,
            Option<string>.None,
            Option<string>.None,
            Option<string>.None,
            FileSystemLayout.FFmpegReportsFolder,
            FileSystemLayout.FontsCacheFolder,
            ffmpegPath);

        FFmpegPipeline pipeline = pipelineBuilder.Seek(inputFile, seek);

        return GetCommand(ffmpegPath, videoInputFile, None, None, None, None, pipeline, false);
    }

    private Command GetCommand(
        string ffmpegPath,
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<ConcatInputFile> concatInputFile,
        Option<GraphicsEngineInput> graphicsEngineInput,
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
            graphicsEngineInput,
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

    private static string GetTonemapAlgorithm(FFmpegPlaybackSettings playbackSettings) =>
        playbackSettings.TonemapAlgorithm switch
        {
            FFmpegProfileTonemapAlgorithm.Linear => TonemapAlgorithm.Linear,
            FFmpegProfileTonemapAlgorithm.Clip => TonemapAlgorithm.Clip,
            FFmpegProfileTonemapAlgorithm.Gamma => TonemapAlgorithm.Gamma,
            FFmpegProfileTonemapAlgorithm.Reinhard => TonemapAlgorithm.Reinhard,
            FFmpegProfileTonemapAlgorithm.Mobius => TonemapAlgorithm.Mobius,
            FFmpegProfileTonemapAlgorithm.Hable => TonemapAlgorithm.Hable,
            _ => throw new ArgumentOutOfRangeException(
                $"unexpected tonemap algorithm {playbackSettings.TonemapAlgorithm}")
        };

    private static Option<string> GetVideoProfile(string videoFormat, string videoProfile) =>
        (videoFormat, (videoProfile ?? string.Empty).ToLowerInvariant()) switch
        {
            (VideoFormat.H264, VideoProfile.Main) => VideoProfile.Main,
            (VideoFormat.H264, VideoProfile.High) => VideoProfile.High,
            (VideoFormat.H264, VideoProfile.High10) => VideoProfile.High10,
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
