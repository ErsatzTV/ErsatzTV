using System.Diagnostics;
using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.GlobalOption;
using ErsatzTV.FFmpeg.InputOption;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.OutputOption;
using ErsatzTV.FFmpeg.OutputOption.Metadata;
using ErsatzTV.FFmpeg.Protocol;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Pipeline;

public abstract class PipelineBuilderBase : IPipelineBuilder
{
    private readonly Option<AudioInputFile> _audioInputFile;
    private readonly IFFmpegCapabilities _ffmpegCapabilities;
    private readonly string _fontsFolder;
    private readonly HardwareAccelerationMode _hardwareAccelerationMode;
    private readonly ILogger _logger;
    private readonly string _reportsFolder;
    private readonly Option<SubtitleInputFile> _subtitleInputFile;
    private readonly Option<VideoInputFile> _videoInputFile;
    private readonly Option<WatermarkInputFile> _watermarkInputFile;

    protected PipelineBuilderBase(
        IFFmpegCapabilities ffmpegCapabilities,
        HardwareAccelerationMode hardwareAccelerationMode,
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        string reportsFolder,
        string fontsFolder,
        ILogger logger)
    {
        _ffmpegCapabilities = ffmpegCapabilities;
        _hardwareAccelerationMode = hardwareAccelerationMode;
        _videoInputFile = videoInputFile;
        _audioInputFile = audioInputFile;
        _watermarkInputFile = watermarkInputFile;
        _subtitleInputFile = subtitleInputFile;
        _reportsFolder = reportsFolder;
        _fontsFolder = fontsFolder;
        _logger = logger;
    }

    public FFmpegPipeline Resize(string outputFile, FrameSize scaledSize)
    {
        var pipelineSteps = new List<IPipelineStep>
        {
            new NoStandardInputOption(),
            new HideBannerOption(),
            new NoStatsOption(),
            new LoglevelErrorOption()
        };

        IPipelineFilterStep scaleStep = new ScaleImageFilter(scaledSize);
        _videoInputFile.Iter(f => f.FilterSteps.Add(scaleStep));

        pipelineSteps.Add(new VideoFilter(new[] { scaleStep }));
        pipelineSteps.Add(scaleStep);
        pipelineSteps.Add(new FileNameOutputOption(outputFile));

        return new FFmpegPipeline(pipelineSteps, false);
    }

    public FFmpegPipeline Concat(ConcatInputFile concatInputFile, FFmpegState ffmpegState)
    {
        var pipelineSteps = new List<IPipelineStep>
        {
            new NoStandardInputOption(),
            new HideBannerOption(),
            new NoStatsOption(),
            new LoglevelErrorOption(),
            new StandardFormatFlags(),
            new NoDemuxDecodeDelayOutputOption(),
            new FastStartOutputOption(),
            new ClosedGopOutputOption()
        };

        concatInputFile.AddOption(new ConcatInputFormat());
        concatInputFile.AddOption(new ReadrateInputOption());
        concatInputFile.AddOption(new InfiniteLoopInputOption(HardwareAccelerationMode.None));

        foreach (int threadCount in ffmpegState.ThreadCount)
        {
            pipelineSteps.Insert(0, new ThreadCountOption(threadCount));
        }

        pipelineSteps.Add(new NoSceneDetectOutputOption(0));
        pipelineSteps.Add(new EncoderCopyAll());

        if (ffmpegState.DoNotMapMetadata)
        {
            pipelineSteps.Add(new DoNotMapMetadataOutputOption());
        }

        pipelineSteps.AddRange(
            ffmpegState.MetadataServiceProvider.Map(sp => new MetadataServiceProviderOutputOption(sp)));

        pipelineSteps.AddRange(ffmpegState.MetadataServiceName.Map(sn => new MetadataServiceNameOutputOption(sn)));

        pipelineSteps.Add(new OutputFormatMpegTs());
        pipelineSteps.Add(new PipeProtocol());

        if (ffmpegState.SaveReport)
        {
            pipelineSteps.Add(new FFReportVariable(_reportsFolder, concatInputFile));
        }

        return new FFmpegPipeline(pipelineSteps, false);
    }

    public FFmpegPipeline WrapSegmenter(ConcatInputFile concatInputFile, FFmpegState ffmpegState)
    {
        var pipelineSteps = new List<IPipelineStep>
        {
            new NoStandardInputOption(),
            new ThreadCountOption(1),
            new HideBannerOption(),
            new LoglevelErrorOption(),
            new NoStatsOption(),
            new StandardFormatFlags(),
            new MapAllStreamsOutputOption(),
            new EncoderCopyAll()
        };

        concatInputFile.AddOption(new ReadrateInputOption());

        SetMetadataServiceProvider(ffmpegState, pipelineSteps);
        SetMetadataServiceName(ffmpegState, pipelineSteps);

        pipelineSteps.Add(new OutputFormatMpegTs(false));
        pipelineSteps.Add(new PipeProtocol());

        return new FFmpegPipeline(pipelineSteps, false);
    }

    public FFmpegPipeline Build(FFmpegState ffmpegState, FrameState desiredState)
    {
        OutputOption.OutputOption outputOption = new FastStartOutputOption();
        if (ffmpegState.OutputFormat == OutputFormatKind.Mp4)
        {
            outputOption = new Mp4OutputOptions();
        }

        var pipelineSteps = new List<IPipelineStep>
        {
            new NoStandardInputOption(),
            new HideBannerOption(),
            new NoStatsOption(),
            new LoglevelErrorOption(),
            new StandardFormatFlags(),
            new NoDemuxDecodeDelayOutputOption(),
            outputOption,
            new ClosedGopOutputOption()
        };

        Debug.Assert(_videoInputFile.IsSome, "Pipeline builder requires exactly one video input file");
        VideoInputFile videoInputFile = _videoInputFile.Head();

        var allVideoStreams = _videoInputFile.SelectMany(f => f.VideoStreams).ToList();
        Debug.Assert(allVideoStreams.Count == 1, "Pipeline builder requires exactly one video stream");
        VideoStream videoStream = allVideoStreams.Head();

        var context = new PipelineContext(
            _hardwareAccelerationMode,
            _watermarkInputFile.IsSome,
            _subtitleInputFile.Map(s => s is { IsImageBased: true, Method: SubtitleMethod.Burn }).IfNone(false),
            _subtitleInputFile.Map(s => s is { IsImageBased: false, Method: SubtitleMethod.Burn }).IfNone(false),
            desiredState.Deinterlaced,
            desiredState.PixelFormat.Map(pf => pf.BitDepth).IfNone(8) == 10,
            false);

        SetThreadCount(ffmpegState, desiredState, pipelineSteps);
        SetSceneDetect(videoStream, ffmpegState, desiredState, pipelineSteps);
        SetFFReport(ffmpegState, pipelineSteps);
        SetStreamSeek(ffmpegState, videoInputFile, context, pipelineSteps);
        SetTimeLimit(ffmpegState, pipelineSteps);

        FilterChain filterChain = BuildVideoPipeline(
            videoInputFile,
            videoStream,
            ffmpegState,
            desiredState,
            context,
            pipelineSteps);

        context = context with { IsIntelVaapiOrQsv = IsIntelVaapiOrQsv(ffmpegState) };

        if (_audioInputFile.IsNone)
        {
            pipelineSteps.Add(new EncoderCopyAudio());
        }
        else
        {
            foreach (AudioInputFile audioInputFile in _audioInputFile)
            {
                BuildAudioPipeline(audioInputFile, pipelineSteps);
            }
        }

        SetDoNotMapMetadata(ffmpegState, pipelineSteps);
        SetMetadataServiceProvider(ffmpegState, pipelineSteps);
        SetMetadataServiceName(ffmpegState, pipelineSteps);
        SetMetadataAudioLanguage(ffmpegState, pipelineSteps);
        SetMetadataSubtitle(ffmpegState, pipelineSteps);
        SetOutputFormat(ffmpegState, desiredState, pipelineSteps, videoStream);

        var complexFilter = new ComplexFilter(
            _videoInputFile,
            _audioInputFile,
            _watermarkInputFile,
            _subtitleInputFile,
            context,
            filterChain);

        pipelineSteps.Add(complexFilter);

        return new FFmpegPipeline(pipelineSteps, context.IsIntelVaapiOrQsv);
    }

    private void LogUnknownDecoder(
        HardwareAccelerationMode hardwareAccelerationMode,
        string videoFormat,
        string pixelFormat) =>
        _logger.LogWarning(
            "Unable to determine decoder for {AccelMode} - {VideoFormat} - {PixelFormat}; may have playback issues",
            hardwareAccelerationMode,
            videoFormat,
            pixelFormat);

    private Option<IEncoder> LogUnknownEncoder(HardwareAccelerationMode hardwareAccelerationMode, string videoFormat)
    {
        _logger.LogWarning(
            "Unable to determine video encoder for {AccelMode} - {VideoFormat}; may have playback issues",
            hardwareAccelerationMode,
            videoFormat);
        return Option<IEncoder>.None;
    }

    private static void SetOutputFormat(
        FFmpegState ffmpegState,
        FrameState desiredState,
        List<IPipelineStep> pipelineSteps,
        VideoStream videoStream)
    {
        switch (ffmpegState.OutputFormat)
        {
            case OutputFormatKind.Mkv:
                pipelineSteps.Add(new OutputFormatMkv());
                pipelineSteps.Add(new PipeProtocol());
                break;
            case OutputFormatKind.MpegTs:
                pipelineSteps.Add(new OutputFormatMpegTs());
                pipelineSteps.Add(new PipeProtocol());
                break;
            case OutputFormatKind.Mp4:
                pipelineSteps.Add(new OutputFormatMp4());
                pipelineSteps.Add(new PipeProtocol());
                break;
            case OutputFormatKind.Hls:
                foreach (string playlistPath in ffmpegState.HlsPlaylistPath)
                {
                    foreach (string segmentTemplate in ffmpegState.HlsSegmentTemplate)
                    {
                        pipelineSteps.Add(
                            new OutputFormatHls(
                                desiredState,
                                videoStream.FrameRate,
                                segmentTemplate,
                                playlistPath));
                    }
                }

                break;
        }
    }

    private static void SetMetadataAudioLanguage(FFmpegState ffmpegState, List<IPipelineStep> pipelineSteps)
    {
        foreach (string desiredAudioLanguage in ffmpegState.MetadataAudioLanguage)
        {
            pipelineSteps.Add(new MetadataAudioLanguageOutputOption(desiredAudioLanguage));
        }
    }

    private static void SetMetadataSubtitle(FFmpegState ffmpegState, List<IPipelineStep> pipelineSteps)
    {
        foreach (string desiredSubtitleLanguage in ffmpegState.MetadataSubtitleLanguage)
        {
            pipelineSteps.Add(new MetadataSubtitleLanguageOutputOption(desiredSubtitleLanguage));
        }

        foreach (string desiredSubtitleTitle in ffmpegState.MetadataSubtitleTitle)
        {
            pipelineSteps.Add(new MetadataSubtitleTitleOutputOption(desiredSubtitleTitle));
        }
    }

    private static void SetMetadataServiceName(FFmpegState ffmpegState, List<IPipelineStep> pipelineSteps)
    {
        foreach (string desiredServiceName in ffmpegState.MetadataServiceName)
        {
            pipelineSteps.Add(new MetadataServiceNameOutputOption(desiredServiceName));
        }
    }

    private static void SetMetadataServiceProvider(FFmpegState ffmpegState, List<IPipelineStep> pipelineSteps)
    {
        foreach (string desiredServiceProvider in ffmpegState.MetadataServiceProvider)
        {
            pipelineSteps.Add(new MetadataServiceProviderOutputOption(desiredServiceProvider));
        }
    }

    private static void SetDoNotMapMetadata(FFmpegState ffmpegState, List<IPipelineStep> pipelineSteps)
    {
        if (ffmpegState.DoNotMapMetadata)
        {
            pipelineSteps.Add(new DoNotMapMetadataOutputOption());
        }
    }

    private void BuildAudioPipeline(AudioInputFile audioInputFile, IList<IPipelineStep> pipelineSteps)
    {
        // always need to specify audio codec so ffmpeg doesn't default to a codec we don't want
        foreach (IEncoder step in AvailableEncoders.ForAudioFormat(audioInputFile.DesiredState, _logger))
        {
            pipelineSteps.Add(step);
        }

        SetAudioChannels(audioInputFile, pipelineSteps);
        SetAudioBitrate(audioInputFile, pipelineSteps);
        SetAudioBufferSize(audioInputFile, pipelineSteps);
        SetAudioSampleRate(audioInputFile, pipelineSteps);
        SetAudioLoudness(audioInputFile);
        SetAudioPad(audioInputFile);
    }

    private void SetAudioPad(AudioInputFile audioInputFile)
    {
        foreach (TimeSpan desiredDuration in audioInputFile.DesiredState.AudioDuration)
        {
            _audioInputFile.Iter(f => f.FilterSteps.Add(new AudioPadFilter(desiredDuration)));
        }
    }

    private void SetAudioLoudness(AudioInputFile audioInputFile)
    {
        if (audioInputFile.DesiredState.NormalizeLoudnessFilter is not AudioFilter.None)
        {
            _audioInputFile.Iter(
                f => f.FilterSteps.Add(
                    new NormalizeLoudnessFilter(audioInputFile.DesiredState.NormalizeLoudnessFilter)));
        }
    }

    private static void SetAudioSampleRate(AudioInputFile audioInputFile, IList<IPipelineStep> pipelineSteps)
    {
        foreach (int desiredSampleRate in audioInputFile.DesiredState.AudioSampleRate)
        {
            pipelineSteps.Add(new AudioSampleRateOutputOption(desiredSampleRate));
        }
    }

    private static void SetAudioBufferSize(AudioInputFile audioInputFile, IList<IPipelineStep> pipelineSteps)
    {
        foreach (int desiredBufferSize in audioInputFile.DesiredState.AudioBufferSize)
        {
            pipelineSteps.Add(new AudioBufferSizeOutputOption(desiredBufferSize));
        }
    }

    private static void SetAudioBitrate(AudioInputFile audioInputFile, IList<IPipelineStep> pipelineSteps)
    {
        foreach (int desiredBitrate in audioInputFile.DesiredState.AudioBitrate)
        {
            pipelineSteps.Add(new AudioBitrateOutputOption(desiredBitrate));
        }
    }

    private static void SetAudioChannels(AudioInputFile audioInputFile, IList<IPipelineStep> pipelineSteps)
    {
        foreach (AudioStream audioStream in audioInputFile.AudioStreams.HeadOrNone())
        {
            foreach (int desiredAudioChannels in audioInputFile.DesiredState.AudioChannels)
            {
                pipelineSteps.Add(
                    new AudioChannelsOutputOption(
                        audioInputFile.DesiredState.AudioFormat,
                        audioStream.Channels,
                        desiredAudioChannels));
            }
        }
    }

    protected abstract bool IsIntelVaapiOrQsv(FFmpegState ffmpegState);

    protected abstract FFmpegState SetAccelState(
        VideoStream videoStream,
        FFmpegState ffmpegState,
        FrameState desiredState,
        PipelineContext context,
        ICollection<IPipelineStep> pipelineSteps);

    private FilterChain BuildVideoPipeline(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        FFmpegState ffmpegState,
        FrameState desiredState,
        PipelineContext context,
        ICollection<IPipelineStep> pipelineSteps)
    {
        foreach (SubtitleInputFile subtitleInputFile in _subtitleInputFile)
        {
            if (subtitleInputFile.Method == SubtitleMethod.Copy)
            {
                pipelineSteps.Add(new EncoderCopySubtitle());
            }
            else if (subtitleInputFile.Method == SubtitleMethod.Convert)
            {
                if (subtitleInputFile.IsImageBased)
                {
                    pipelineSteps.Add(new EncoderDvdSubtitle());
                }
            }
        }

        ffmpegState = SetAccelState(videoStream, ffmpegState, desiredState, context, pipelineSteps);

        // don't use explicit decoder with HLS Direct
        Option<IDecoder> maybeDecoder = desiredState.VideoFormat == VideoFormat.Copy
            ? None
            : SetDecoder(videoInputFile, videoStream, ffmpegState, context);

        SetStillImageInfiniteLoop(videoInputFile, videoStream, ffmpegState);
        SetRealtimeInput(videoInputFile, desiredState);
        SetInfiniteLoop(videoInputFile, videoStream, ffmpegState, desiredState);
        SetFrameRateOutput(desiredState, pipelineSteps);
        SetVideoTrackTimescaleOutput(desiredState, pipelineSteps);
        SetVideoBitrateOutput(desiredState, pipelineSteps);
        SetVideoBufferSizeOutput(desiredState, pipelineSteps);

        FilterChain filterChain = SetVideoFilters(
            videoInputFile,
            videoStream,
            _watermarkInputFile,
            _subtitleInputFile,
            context,
            maybeDecoder,
            ffmpegState,
            desiredState,
            _fontsFolder,
            pipelineSteps);

        SetOutputTsOffset(ffmpegState, desiredState, pipelineSteps);

        return filterChain;
    }

    protected abstract Option<IDecoder> SetDecoder(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        FFmpegState ffmpegState,
        PipelineContext context);

    protected Option<IDecoder> GetSoftwareDecoder(VideoStream videoStream)
    {
        Option<IDecoder> maybeDecoder = _ffmpegCapabilities.SoftwareDecoderForVideoFormat(videoStream.Codec);
        if (maybeDecoder.IsNone)
        {
            LogUnknownDecoder(
                HardwareAccelerationMode.None,
                videoStream.Codec,
                videoStream.PixelFormat.Match(pf => pf.Name, () => string.Empty));
        }

        return maybeDecoder;
    }

    protected Option<IEncoder> GetSoftwareEncoder(FrameState currentState, FrameState desiredState) =>
        desiredState.VideoFormat switch
        {
            VideoFormat.Hevc => new EncoderLibx265(
                currentState with { FrameDataLocation = FrameDataLocation.Software }),
            VideoFormat.H264 => new EncoderLibx264(),
            VideoFormat.Mpeg2Video => new EncoderMpeg2Video(),

            VideoFormat.Copy => new EncoderCopyVideo(),
            VideoFormat.Undetermined => new EncoderImplicitVideo(),

            _ => LogUnknownEncoder(HardwareAccelerationMode.None, desiredState.VideoFormat)
        };

    protected abstract FilterChain SetVideoFilters(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        PipelineContext context,
        Option<IDecoder> maybeDecoder,
        FFmpegState ffmpegState,
        FrameState desiredState,
        string fontsFolder,
        ICollection<IPipelineStep> pipelineSteps);
    
    protected static FrameState SetCrop(
        VideoInputFile videoInputFile,
        FrameState desiredState,
        FrameState currentState)
    {
        foreach (FrameSize croppedSize in currentState.CroppedSize)
        {
            IPipelineFilterStep cropStep = new CropFilter(currentState, croppedSize);
            currentState = cropStep.NextState(currentState);
            videoInputFile.FilterSteps.Add(cropStep);
        }

        return currentState;
    }

    private static void SetOutputTsOffset(
        FFmpegState ffmpegState,
        FrameState desiredState,
        ICollection<IPipelineStep> pipelineSteps)
    {
        if (desiredState.VideoFormat == VideoFormat.Copy)
        {
            return;
        }

        if (ffmpegState.PtsOffset > 0)
        {
            foreach (int videoTrackTimeScale in desiredState.VideoTrackTimeScale)
            {
                pipelineSteps.Add(new OutputTsOffsetOption(ffmpegState.PtsOffset, videoTrackTimeScale));
            }
        }
    }

    private static void SetVideoBufferSizeOutput(FrameState desiredState, ICollection<IPipelineStep> pipelineSteps)
    {
        if (desiredState.VideoFormat == VideoFormat.Copy)
        {
            return;
        }

        foreach (int desiredBufferSize in desiredState.VideoBufferSize)
        {
            pipelineSteps.Add(new VideoBufferSizeOutputOption(desiredBufferSize));
        }
    }

    private static void SetVideoBitrateOutput(FrameState desiredState, ICollection<IPipelineStep> pipelineSteps)
    {
        if (desiredState.VideoFormat == VideoFormat.Copy)
        {
            return;
        }

        foreach (int desiredBitrate in desiredState.VideoBitrate)
        {
            pipelineSteps.Add(new VideoBitrateOutputOption(desiredBitrate));
        }
    }

    private static void SetVideoTrackTimescaleOutput(FrameState desiredState, ICollection<IPipelineStep> pipelineSteps)
    {
        if (desiredState.VideoFormat == VideoFormat.Copy)
        {
            return;
        }

        foreach (int desiredTimeScale in desiredState.VideoTrackTimeScale)
        {
            pipelineSteps.Add(new VideoTrackTimescaleOutputOption(desiredTimeScale));
        }
    }

    private static void SetFrameRateOutput(FrameState desiredState, ICollection<IPipelineStep> pipelineSteps)
    {
        if (desiredState.VideoFormat == VideoFormat.Copy)
        {
            return;
        }

        foreach (int desiredFrameRate in desiredState.FrameRate)
        {
            pipelineSteps.Add(new FrameRateOutputOption(desiredFrameRate));
        }
    }

    private void SetInfiniteLoop(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        FFmpegState ffmpegState,
        FrameState desiredState)
    {
        if (!desiredState.InfiniteLoop)
        {
            return;
        }

        foreach (AudioInputFile audioInputFile in _audioInputFile)
        {
            audioInputFile.AddOption(new InfiniteLoopInputOption(ffmpegState.EncoderHardwareAccelerationMode));
        }

        if (!videoStream.StillImage)
        {
            videoInputFile.AddOption(new InfiniteLoopInputOption(ffmpegState.EncoderHardwareAccelerationMode));
        }
    }

    private void SetRealtimeInput(VideoInputFile videoInputFile, FrameState desiredState)
    {
        int initialBurst;
        if (!desiredState.Realtime)
        {
            initialBurst = 180;
        }
        else
        {
            AudioFilter filter = _audioInputFile
                .Map(a => a.DesiredState.NormalizeLoudnessFilter)
                .IfNone(AudioFilter.None);
                
            initialBurst = filter switch
            {
                AudioFilter.LoudNorm => 5,
                AudioFilter.DynAudNorm => 15,
                _ => 0
            };
        }

        _audioInputFile.Iter(a => a.AddOption(new ReadrateInputOption(initialBurst)));
        videoInputFile.AddOption(new ReadrateInputOption(initialBurst));
    }

    private static void SetStillImageInfiniteLoop(
        VideoInputFile videoInputFile,
        VideoStream videoStream,
        FFmpegState ffmpegState)
    {
        if (videoStream.StillImage)
        {
            videoInputFile.AddOption(new InfiniteLoopInputOption(ffmpegState.EncoderHardwareAccelerationMode));
        }
    }

    private void SetThreadCount(FFmpegState ffmpegState, FrameState desiredState, IList<IPipelineStep> pipelineSteps)
    {
        if (ffmpegState.DecoderHardwareAccelerationMode != HardwareAccelerationMode.None ||
            ffmpegState.EncoderHardwareAccelerationMode != HardwareAccelerationMode.None)
        {
            _logger.LogInformation(
                "Forcing {Threads} ffmpeg thread when hardware acceleration is used",
                1);

            pipelineSteps.Insert(0, new ThreadCountOption(1));
        }
        else if (ffmpegState.Start.Exists(s => s > TimeSpan.Zero) && desiredState.Realtime)
        {
            _logger.LogInformation(
                "Forcing {Threads} ffmpeg thread due to buggy combination of stream seek and realtime output",
                1);

            pipelineSteps.Insert(0, new ThreadCountOption(1));
        }
        else
        {
            foreach (int threadCount in ffmpegState.ThreadCount)
            {
                pipelineSteps.Insert(0, new ThreadCountOption(threadCount));
            }
        }
    }

    private static void SetSceneDetect(
        // ReSharper disable once SuggestBaseTypeForParameter
        VideoStream videoStream,
        FFmpegState ffmpegState,
        FrameState desiredState,
        ICollection<IPipelineStep> pipelineSteps)
    {
        // -sc_threshold 0 is unsupported with mpeg2video
        if (videoStream.Codec == VideoFormat.Mpeg2Video || desiredState.VideoFormat == VideoFormat.Mpeg2Video ||
            ffmpegState.DecoderHardwareAccelerationMode == HardwareAccelerationMode.VideoToolbox)
        {
            pipelineSteps.Add(new NoSceneDetectOutputOption(1_000_000_000));
        }
        else
        {
            pipelineSteps.Add(new NoSceneDetectOutputOption(0));
        }
    }

    private void SetFFReport(FFmpegState ffmpegState, ICollection<IPipelineStep> pipelineSteps)
    {
        if (ffmpegState.SaveReport)
        {
            pipelineSteps.Add(new FFReportVariable(_reportsFolder, None));
        }
    }

    private void SetStreamSeek(
        FFmpegState ffmpegState,
        VideoInputFile videoInputFile,
        PipelineContext context,
        ICollection<IPipelineStep> pipelineSteps)
    {
        foreach (TimeSpan desiredStart in ffmpegState.Start.Filter(s => s > TimeSpan.Zero))
        {
            var option = new StreamSeekInputOption(desiredStart);
            _audioInputFile.Iter(a => a.AddOption(option));
            videoInputFile.AddOption(option);

            // need to seek text subtitle files
            if (context.HasSubtitleText)
            {
                pipelineSteps.Add(new StreamSeekFilterOption(desiredStart));
            }
        }
    }

    private static void SetTimeLimit(FFmpegState ffmpegState, List<IPipelineStep> pipelineSteps) =>
        pipelineSteps.AddRange(ffmpegState.Finish.Map(finish => new TimeLimitOutputOption(finish)));
}
