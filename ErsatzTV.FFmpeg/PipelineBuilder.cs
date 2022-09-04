using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Filter.Cuda;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.Option;
using ErsatzTV.FFmpeg.Option.HardwareAcceleration;
using ErsatzTV.FFmpeg.Option.Metadata;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.Protocol;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg;

public class PipelineBuilder
{
    private readonly Option<AudioInputFile> _audioInputFile;
    private readonly string _fontsFolder;
    private readonly IRuntimeInfo _runtimeInfo;
    private readonly IHardwareCapabilities _hardwareCapabilities;
    private readonly ILogger _logger;
    private readonly List<IPipelineStep> _pipelineSteps;
    private readonly string _reportsFolder;
    private readonly Option<SubtitleInputFile> _subtitleInputFile;
    private readonly Option<VideoInputFile> _videoInputFile;
    private readonly Option<WatermarkInputFile> _watermarkInputFile;

    public PipelineBuilder(
        IRuntimeInfo runtimeInfo,
        IHardwareCapabilities hardwareCapabilities,
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        string reportsFolder,
        string fontsFolder,
        ILogger logger)
    {
        _pipelineSteps = new List<IPipelineStep>
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

        _runtimeInfo = runtimeInfo;
        _hardwareCapabilities = hardwareCapabilities;
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
        _pipelineSteps.Clear();
        _pipelineSteps.Add(new NoStandardInputOption());
        _pipelineSteps.Add(new HideBannerOption());
        _pipelineSteps.Add(new NoStatsOption());
        _pipelineSteps.Add(new LoglevelErrorOption());

        IPipelineFilterStep scaleStep = new ScaleImageFilter(scaledSize);
        _videoInputFile.Iter(f => f.FilterSteps.Add(scaleStep));

        _pipelineSteps.Add(new VideoFilter(new[] { scaleStep }));
        _pipelineSteps.Add(scaleStep);
        _pipelineSteps.Add(new FileNameOutputOption(outputFile));

        return new FFmpegPipeline(_pipelineSteps);
    }

    public FFmpegPipeline Concat(ConcatInputFile concatInputFile, FFmpegState ffmpegState)
    {
        concatInputFile.AddOption(new ConcatInputFormat());
        concatInputFile.AddOption(new RealtimeInputOption());
        concatInputFile.AddOption(new InfiniteLoopInputOption(HardwareAccelerationMode.None));

        foreach (int threadCount in ffmpegState.ThreadCount)
        {
            _pipelineSteps.Insert(0, new ThreadCountOption(threadCount));
        }

        _pipelineSteps.Add(new NoSceneDetectOutputOption(0));
        _pipelineSteps.Add(new EncoderCopyAll());

        if (ffmpegState.DoNotMapMetadata)
        {
            _pipelineSteps.Add(new DoNotMapMetadataOutputOption());
        }

        foreach (string desiredServiceProvider in ffmpegState.MetadataServiceProvider)
        {
            _pipelineSteps.Add(new MetadataServiceProviderOutputOption(desiredServiceProvider));
        }

        foreach (string desiredServiceName in ffmpegState.MetadataServiceName)
        {
            _pipelineSteps.Add(new MetadataServiceNameOutputOption(desiredServiceName));
        }

        _pipelineSteps.Add(new OutputFormatMpegTs());
        _pipelineSteps.Add(new PipeProtocol());

        if (ffmpegState.SaveReport)
        {
            _pipelineSteps.Add(new FFReportVariable(_reportsFolder, concatInputFile));
        }

        return new FFmpegPipeline(_pipelineSteps);
    }

    public FFmpegPipeline Build(FFmpegState ffmpegState, FrameState desiredState)
    {
        if (ffmpegState.Start.Exists(s => s > TimeSpan.Zero) && desiredState.Realtime)
        {
            _logger.LogInformation(
                "Forcing {Threads} ffmpeg thread due to buggy combination of stream seek and realtime output",
                1);

            _pipelineSteps.Insert(0, new ThreadCountOption(1));
        }
        else
        {
            foreach (int threadCount in ffmpegState.ThreadCount)
            {
                _pipelineSteps.Insert(0, new ThreadCountOption(threadCount));
            }
        }

        var allVideoStreams = _videoInputFile.SelectMany(f => f.VideoStreams).ToList();

        // -sc_threshold 0 is unsupported with mpeg2video
        _pipelineSteps.Add(
            allVideoStreams.All(s => s.Codec != VideoFormat.Mpeg2Video) &&
            desiredState.VideoFormat != VideoFormat.Mpeg2Video
                ? new NoSceneDetectOutputOption(0)
                : new NoSceneDetectOutputOption(1_000_000_000));

        if (ffmpegState.SaveReport)
        {
            _pipelineSteps.Add(new FFReportVariable(_reportsFolder, None));
        }

        foreach (TimeSpan desiredStart in ffmpegState.Start.Filter(s => s > TimeSpan.Zero))
        {
            var option = new StreamSeekInputOption(desiredStart);
            _audioInputFile.Iter(f => f.AddOption(option));
            _videoInputFile.Iter(f => f.AddOption(option));

            // need to seek text subtitle files
            if (_subtitleInputFile.Map(s => !s.IsImageBased).IfNone(false))
            {
                _pipelineSteps.Add(new StreamSeekFilterOption(desiredStart));
            }
        }

        foreach (TimeSpan desiredFinish in ffmpegState.Finish)
        {
            _pipelineSteps.Add(new TimeLimitOutputOption(desiredFinish));
        }

        foreach (VideoStream videoStream in allVideoStreams)
        {
            bool hasOverlay = _watermarkInputFile.IsSome ||
                              _subtitleInputFile.Map(s => s.IsImageBased && !s.Copy).IfNone(false);

            Option<int> initialFrameRate = Option<int>.None;
            foreach (string frameRateString in videoStream.FrameRate)
            {
                if (int.TryParse(frameRateString, out int parsedFrameRate))
                {
                    initialFrameRate = parsedFrameRate;
                }
            }

            var currentState = new FrameState(
                false, // realtime
                false, // infinite loop
                videoStream.Codec,
                videoStream.PixelFormat,
                videoStream.FrameSize,
                videoStream.FrameSize,
                videoStream.DisplayAspectRatio,
                initialFrameRate,
                Option<int>.None,
                Option<int>.None,
                Option<int>.None,
                false); // deinterlace

            IEncoder encoder;

            if (IsDesiredVideoState(currentState, desiredState))
            {
                encoder = new EncoderCopyVideo();
                _pipelineSteps.Add(encoder);
            }
            else
            {
                Option<IPipelineStep> maybeAccel = AvailableHardwareAccelerationOptions.ForMode(
                    ffmpegState.EncoderHardwareAccelerationMode,
                    ffmpegState.VaapiDevice,
                    _logger);

                if (maybeAccel.IsNone)
                {
                    ffmpegState = ffmpegState with
                    {
                        // disable hw accel if we don't match anything
                        DecoderHardwareAccelerationMode = HardwareAccelerationMode.None,
                        EncoderHardwareAccelerationMode = HardwareAccelerationMode.None
                    };
                }

                foreach (IPipelineStep accel in maybeAccel)
                {
                    bool canDecode = _hardwareCapabilities.CanDecode(currentState.VideoFormat, videoStream.PixelFormat);
                    bool canEncode = _hardwareCapabilities.CanEncode(
                        desiredState.VideoFormat,
                        desiredState.PixelFormat);

                    // disable hw accel if decoder/encoder isn't supported
                    if (!canDecode || !canEncode)
                    {
                        ffmpegState = ffmpegState with
                        {
                            DecoderHardwareAccelerationMode = canDecode
                                ? ffmpegState.DecoderHardwareAccelerationMode
                                : HardwareAccelerationMode.None,
                            EncoderHardwareAccelerationMode = canEncode
                                ? ffmpegState.EncoderHardwareAccelerationMode
                                : HardwareAccelerationMode.None
                        };
                    }

                    if (canDecode || canEncode)
                    {
                        currentState = accel.NextState(currentState);
                        _pipelineSteps.Add(accel);
                    }
                }

                // nvenc requires yuv420p background with yuva420p overlay
                if (ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Nvenc && hasOverlay)
                {
                    desiredState = desiredState with { PixelFormat = new PixelFormatYuv420P() };
                }

                // qsv should stay nv12
                if (ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Qsv && hasOverlay)
                {
                    IPixelFormat pixelFormat = desiredState.PixelFormat.IfNone(new PixelFormatYuv420P());
                    desiredState = desiredState with { PixelFormat = new PixelFormatNv12(pixelFormat.Name) };
                }

                foreach (string desiredVaapiDriver in ffmpegState.VaapiDriver)
                {
                    IPipelineStep step = new LibvaDriverNameVariable(desiredVaapiDriver);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }

                foreach (IDecoder decoder in AvailableDecoders.ForVideoFormat(
                             _hardwareCapabilities,
                             ffmpegState,
                             currentState,
                             desiredState,
                             _watermarkInputFile,
                             _subtitleInputFile,
                             _logger))
                {
                    foreach (VideoInputFile videoInputFile in _videoInputFile)
                    {
                        videoInputFile.AddOption(decoder);
                        currentState = decoder.NextState(currentState);
                    }
                }
            }

            if (_subtitleInputFile.Map(s => s.Copy) == Some(true))
            {
                _pipelineSteps.Add(new EncoderCopySubtitle());
            }

            if (videoStream.StillImage)
            {
                var option = new InfiniteLoopInputOption(ffmpegState.EncoderHardwareAccelerationMode);
                _videoInputFile.Iter(f => f.AddOption(option));
            }

            if (!IsDesiredVideoState(currentState, desiredState))
            {
                if (desiredState.Realtime)
                {
                    var option = new RealtimeInputOption();
                    _audioInputFile.Iter(f => f.AddOption(option));
                    _videoInputFile.Iter(f => f.AddOption(option));
                }

                if (desiredState.InfiniteLoop)
                {
                    var option = new InfiniteLoopInputOption(ffmpegState.EncoderHardwareAccelerationMode);
                    _audioInputFile.Iter(f => f.AddOption(option));
                    _videoInputFile.Iter(f => f.AddOption(option));
                }

                foreach (int desiredFrameRate in desiredState.FrameRate)
                {
                    if (currentState.FrameRate != desiredFrameRate)
                    {
                        IPipelineStep step = new FrameRateOutputOption(desiredFrameRate);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (int desiredTimeScale in desiredState.VideoTrackTimeScale)
                {
                    if (currentState.VideoTrackTimeScale != desiredTimeScale)
                    {
                        IPipelineStep step = new VideoTrackTimescaleOutputOption(desiredTimeScale);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (int desiredBitrate in desiredState.VideoBitrate)
                {
                    if (currentState.VideoBitrate != desiredBitrate)
                    {
                        IPipelineStep step = new VideoBitrateOutputOption(desiredBitrate);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (int desiredBufferSize in desiredState.VideoBufferSize)
                {
                    if (currentState.VideoBufferSize != desiredBufferSize)
                    {
                        IPipelineStep step = new VideoBufferSizeOutputOption(desiredBufferSize);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                if (desiredState.Deinterlaced && !currentState.Deinterlaced)
                {
                    IPipelineFilterStep step = AvailableDeinterlaceFilters.ForAcceleration(
                        ffmpegState.EncoderHardwareAccelerationMode,
                        currentState,
                        desiredState,
                        _watermarkInputFile,
                        _subtitleInputFile);
                    currentState = step.NextState(currentState);
                    _videoInputFile.Iter(f => f.FilterSteps.Add(step));
                }

                // TODO: this is a software-only flow, will need to be different for hardware accel
                if (ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.None)
                {
                    if (currentState.ScaledSize != desiredState.ScaledSize ||
                        currentState.PaddedSize != desiredState.PaddedSize)
                    {
                        IPipelineFilterStep scaleStep = new ScaleFilter(
                            currentState,
                            desiredState.ScaledSize,
                            desiredState.PaddedSize);
                        currentState = scaleStep.NextState(currentState);
                        _videoInputFile.Iter(f => f.FilterSteps.Add(scaleStep));

                        // TODO: padding might not be needed, can we optimize this out?
                        IPipelineFilterStep padStep = new PadFilter(currentState, desiredState.PaddedSize);
                        currentState = padStep.NextState(currentState);
                        _videoInputFile.Iter(f => f.FilterSteps.Add(padStep));

                        if (videoStream.DisplayAspectRatio == desiredState.DisplayAspectRatio)
                        {
                            IPipelineFilterStep darStep = new SetDarFilter(desiredState.DisplayAspectRatio);
                            currentState = darStep.NextState(currentState);
                            _videoInputFile.Iter(f => f.FilterSteps.Add(darStep));
                        }
                    }
                }
                else if (currentState.ScaledSize != desiredState.ScaledSize)
                {
                    IPipelineFilterStep scaleFilter = AvailableScaleFilters.ForAcceleration(
                        _runtimeInfo,
                        ffmpegState.EncoderHardwareAccelerationMode,
                        currentState,
                        desiredState.ScaledSize,
                        desiredState.PaddedSize);
                    currentState = scaleFilter.NextState(currentState);
                    _videoInputFile.Iter(f => f.FilterSteps.Add(scaleFilter));

                    // TODO: padding might not be needed, can we optimize this out?
                    if (currentState.PaddedSize != desiredState.PaddedSize)
                    {
                        IPipelineFilterStep padStep = new PadFilter(currentState, desiredState.PaddedSize);
                        currentState = padStep.NextState(currentState);
                        _videoInputFile.Iter(f => f.FilterSteps.Add(padStep));
                    }

                    if (videoStream.DisplayAspectRatio == desiredState.DisplayAspectRatio ||
                        ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Qsv)
                    {
                        IPipelineFilterStep darStep = new SetDarFilter(desiredState.DisplayAspectRatio);
                        currentState = darStep.NextState(currentState);
                        _videoInputFile.Iter(f => f.FilterSteps.Add(darStep));
                    }
                }
                else if (currentState.PaddedSize != desiredState.PaddedSize)
                {
                    IPipelineFilterStep scaleFilter = AvailableScaleFilters.ForAcceleration(
                        _runtimeInfo,
                        ffmpegState.EncoderHardwareAccelerationMode,
                        currentState,
                        desiredState.ScaledSize,
                        desiredState.PaddedSize);
                    currentState = scaleFilter.NextState(currentState);
                    _videoInputFile.Iter(f => f.FilterSteps.Add(scaleFilter));

                    if (currentState.PaddedSize != desiredState.PaddedSize)
                    {
                        IPipelineFilterStep padStep = new PadFilter(currentState, desiredState.PaddedSize);
                        currentState = padStep.NextState(currentState);
                        _videoInputFile.Iter(f => f.FilterSteps.Add(padStep));
                    }

                    if (videoStream.DisplayAspectRatio == desiredState.DisplayAspectRatio ||
                        ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Qsv)
                    {
                        IPipelineFilterStep darStep = new SetDarFilter(desiredState.DisplayAspectRatio);
                        currentState = darStep.NextState(currentState);
                        _videoInputFile.Iter(f => f.FilterSteps.Add(darStep));
                    }
                }

                if (hasOverlay && currentState.PixelFormat.Map(pf => pf.FFmpegName) !=
                    desiredState.PixelFormat.Map(pf => pf.FFmpegName))
                {
                    // this should only happen with nvenc?
                    // use scale filter to fix pixel format

                    foreach (IPixelFormat pixelFormat in desiredState.PixelFormat)
                    {
                        if (currentState.FrameDataLocation == FrameDataLocation.Software)
                        {
                            IPipelineFilterStep formatFilter = new PixelFormatFilter(pixelFormat);
                            currentState = formatFilter.NextState(currentState);
                            _videoInputFile.Iter(f => f.FilterSteps.Add(formatFilter));

                            switch (ffmpegState.EncoderHardwareAccelerationMode)
                            {
                                case HardwareAccelerationMode.Nvenc:
                                    var uploadFilter = new HardwareUploadFilter(ffmpegState);
                                    currentState = uploadFilter.NextState(currentState);
                                    _videoInputFile.Iter(f => f.FilterSteps.Add(uploadFilter));
                                    break;
                            }
                        }
                        else
                        {
                            if (ffmpegState.EncoderHardwareAccelerationMode != HardwareAccelerationMode.Qsv)
                            {
                                // the filter re-applies the current pixel format, so we have to set it first
                                currentState = currentState with { PixelFormat = desiredState.PixelFormat };

                                IPipelineFilterStep scaleFilter = AvailableScaleFilters.ForAcceleration(
                                    _runtimeInfo,
                                    ffmpegState.EncoderHardwareAccelerationMode,
                                    currentState,
                                    desiredState.ScaledSize,
                                    desiredState.PaddedSize);
                                currentState = scaleFilter.NextState(currentState);
                                _videoInputFile.Iter(f => f.FilterSteps.Add(scaleFilter));
                            }
                        }
                    }
                }

                // nvenc custom logic
                if (ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Nvenc)
                {
                    foreach (VideoInputFile videoInputFile in _videoInputFile)
                    {
                        // if we only deinterlace, we need to set pixel format again (using scale_cuda)
                        bool onlyYadif = videoInputFile.FilterSteps.Count == 1 &&
                                         videoInputFile.FilterSteps.Any(fs => fs is YadifCudaFilter);

                        // if we have no filters and an overlay, we need to set pixel format
                        bool unfilteredWithOverlay = videoInputFile.FilterSteps.Count == 0 && hasOverlay;

                        if (onlyYadif || unfilteredWithOverlay)
                        {
                            // the filter re-applies the current pixel format, so we have to set it first
                            currentState = currentState with { PixelFormat = desiredState.PixelFormat };

                            IPipelineFilterStep scaleFilter = AvailableScaleFilters.ForAcceleration(
                                _runtimeInfo,
                                ffmpegState.EncoderHardwareAccelerationMode,
                                currentState,
                                desiredState.ScaledSize,
                                desiredState.PaddedSize);
                            currentState = scaleFilter.NextState(currentState);
                            videoInputFile.FilterSteps.Add(scaleFilter);
                        }
                    }
                }

                if (ffmpegState.PtsOffset > 0)
                {
                    foreach (int videoTrackTimeScale in desiredState.VideoTrackTimeScale)
                    {
                        IPipelineStep step = new OutputTsOffsetOption(
                            ffmpegState.PtsOffset,
                            videoTrackTimeScale);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (IPixelFormat desiredPixelFormat in desiredState.PixelFormat)
                {
                    if (currentState.PixelFormat.Map(pf => pf.FFmpegName) != desiredPixelFormat.FFmpegName)
                    {
                        // qsv doesn't seem to like this
                        if (ffmpegState.EncoderHardwareAccelerationMode != HardwareAccelerationMode.Qsv)
                        {
                            IPipelineStep step = new PixelFormatOutputOption(desiredPixelFormat);
                            currentState = step.NextState(currentState);
                            _pipelineSteps.Add(step);
                        }
                    }
                }
            }

            // TODO: if all video filters are software, use software pixel format for hwaccel output
            // might be able to skip scale_cuda=format=whatever,hwdownload,format=whatever

            if (_audioInputFile.IsNone)
            {
                // always need to specify audio codec so ffmpeg doesn't default to a codec we don't want
                foreach (IEncoder step in AvailableEncoders.ForAudioFormat(AudioState.Copy, _logger))
                {
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }
            }

            foreach (AudioInputFile audioInputFile in _audioInputFile)
            {
                // always need to specify audio codec so ffmpeg doesn't default to a codec we don't want
                foreach (IEncoder step in AvailableEncoders.ForAudioFormat(audioInputFile.DesiredState, _logger))
                {
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }

                foreach (AudioStream audioStream in audioInputFile.AudioStreams.HeadOrNone())
                {
                    foreach (int desiredAudioChannels in audioInputFile.DesiredState.AudioChannels)
                    {
                        _pipelineSteps.Add(
                            new AudioChannelsOutputOption(
                                audioInputFile.DesiredState.AudioFormat,
                                audioStream.Channels,
                                desiredAudioChannels));
                    }
                }

                foreach (int desiredBitrate in audioInputFile.DesiredState.AudioBitrate)
                {
                    _pipelineSteps.Add(new AudioBitrateOutputOption(desiredBitrate));
                }

                foreach (int desiredBufferSize in audioInputFile.DesiredState.AudioBufferSize)
                {
                    _pipelineSteps.Add(new AudioBufferSizeOutputOption(desiredBufferSize));
                }

                foreach (int desiredSampleRate in audioInputFile.DesiredState.AudioSampleRate)
                {
                    _pipelineSteps.Add(new AudioSampleRateOutputOption(desiredSampleRate));
                }

                if (audioInputFile.DesiredState.NormalizeLoudness)
                {
                    _audioInputFile.Iter(f => f.FilterSteps.Add(new NormalizeLoudnessFilter()));
                }

                foreach (TimeSpan desiredDuration in audioInputFile.DesiredState.AudioDuration)
                {
                    _audioInputFile.Iter(f => f.FilterSteps.Add(new AudioPadFilter(desiredDuration)));
                }
            }

            foreach (SubtitleInputFile subtitleInputFile in _subtitleInputFile)
            {
                if (subtitleInputFile.IsImageBased)
                {
                    // vaapi and videotoolbox use a software overlay, so we need to ensure the background is already in software
                    // though videotoolbox uses software decoders, so no need to download for that
                    if (ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Vaapi)
                    {
                        var downloadFilter = new HardwareDownloadFilter(currentState);
                        currentState = downloadFilter.NextState(currentState);
                        _videoInputFile.Iter(f => f.FilterSteps.Add(downloadFilter));
                    }

                    var pixelFormatFilter = new SubtitlePixelFormatFilter(ffmpegState);
                    subtitleInputFile.FilterSteps.Add(pixelFormatFilter);

                    subtitleInputFile.FilterSteps.Add(new SubtitleHardwareUploadFilter(currentState, ffmpegState));

                    FrameState fakeState = currentState;
                    foreach (string format in pixelFormatFilter.MaybeFormat)
                    {
                        fakeState = fakeState with
                        {
                            PixelFormat = AvailablePixelFormats.ForPixelFormat(format, _logger)
                        };
                    }

                    // hacky check for actual scaling or padding
                    if (_videoInputFile.Exists(
                            v => v.FilterSteps.Any(s => s.Filter.Contains(currentState.PaddedSize.Height.ToString()))))
                    {
                        // enable scaling the subtitle stream
                        fakeState = fakeState with { ScaledSize = new FrameSize(1, 1) };
                    }

                    IPipelineFilterStep scaleFilter = AvailableSubtitleScaleFilters.ForAcceleration(
                        _runtimeInfo,
                        ffmpegState.EncoderHardwareAccelerationMode,
                        fakeState,
                        desiredState.ScaledSize,
                        desiredState.PaddedSize);
                    subtitleInputFile.FilterSteps.Add(scaleFilter);
                }
                else
                {
                    _videoInputFile.Iter(f => f.AddOption(new CopyTimestampInputOption()));

                    // text-based subtitles are always added in software, so always try to download the background

                    // nvidia needs some extra format help if the only filter will be the download filter
                    if (ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Nvenc &&
                        currentState.FrameDataLocation == FrameDataLocation.Hardware &&
                        _videoInputFile.Map(f => f.FilterSteps.Count).IfNone(1) == 0)
                    {
                        IPipelineFilterStep scaleFilter = AvailableScaleFilters.ForAcceleration(
                            _runtimeInfo,
                            ffmpegState.EncoderHardwareAccelerationMode,
                            currentState,
                            desiredState.ScaledSize,
                            desiredState.PaddedSize);
                        currentState = scaleFilter.NextState(currentState);
                        _videoInputFile.Iter(f => f.FilterSteps.Add(scaleFilter));
                    }

                    var downloadFilter = new HardwareDownloadFilter(currentState);
                    currentState = downloadFilter.NextState(currentState);
                    _videoInputFile.Iter(f => f.FilterSteps.Add(downloadFilter));
                }
            }

            foreach (WatermarkInputFile watermarkInputFile in _watermarkInputFile)
            {
                // vaapi and videotoolbox use a software overlay, so we need to ensure the background is already in software
                // though videotoolbox uses software decoders, so no need to download for that
                if (ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.Vaapi)
                {
                    var downloadFilter = new HardwareDownloadFilter(currentState);
                    currentState = downloadFilter.NextState(currentState);
                    _videoInputFile.Iter(f => f.FilterSteps.Add(downloadFilter));
                }

                watermarkInputFile.FilterSteps.Add(
                    new WatermarkPixelFormatFilter(ffmpegState, watermarkInputFile.DesiredState));

                foreach (VideoStream watermarkStream in watermarkInputFile.VideoStreams)
                {
                    if (watermarkStream.StillImage == false)
                    {
                        watermarkInputFile.AddOption(new DoNotIgnoreLoopInputOption());
                    }
                    else if (watermarkInputFile.DesiredState.MaybeFadePoints.Map(fp => fp.Count > 0).IfNone(false))
                    {
                        // looping is required to fade a static image in and out
                        watermarkInputFile.AddOption(
                            new InfiniteLoopInputOption(ffmpegState.EncoderHardwareAccelerationMode));
                    }
                }

                if (watermarkInputFile.DesiredState.Size == WatermarkSize.Scaled)
                {
                    watermarkInputFile.FilterSteps.Add(
                        new WatermarkScaleFilter(watermarkInputFile.DesiredState, currentState.PaddedSize));
                }

                if (watermarkInputFile.DesiredState.Opacity != 100)
                {
                    watermarkInputFile.FilterSteps.Add(new WatermarkOpacityFilter(watermarkInputFile.DesiredState));
                }

                foreach (List<WatermarkFadePoint> fadePoints in watermarkInputFile.DesiredState.MaybeFadePoints)
                {
                    watermarkInputFile.FilterSteps.AddRange(fadePoints.Map(fp => new WatermarkFadeFilter(fp)));
                }

                watermarkInputFile.FilterSteps.Add(new WatermarkHardwareUploadFilter(currentState, ffmpegState));
            }

            // after everything else is done, apply the encoder
            if (_pipelineSteps.OfType<IEncoder>().All(e => e.Kind != StreamKind.Video))
            {
                foreach (IEncoder e in AvailableEncoders.ForVideoFormat(
                             _hardwareCapabilities,
                             ffmpegState,
                             currentState,
                             desiredState,
                             _watermarkInputFile,
                             _subtitleInputFile,
                             _logger))
                {
                    encoder = e;
                    _pipelineSteps.Add(encoder);
                    _videoInputFile.Iter(f => f.FilterSteps.Add(encoder));
                    currentState = encoder.NextState(currentState);
                }
            }

            if (ffmpegState.DoNotMapMetadata)
            {
                _pipelineSteps.Add(new DoNotMapMetadataOutputOption());
            }

            foreach (string desiredServiceProvider in ffmpegState.MetadataServiceProvider)
            {
                _pipelineSteps.Add(new MetadataServiceProviderOutputOption(desiredServiceProvider));
            }

            foreach (string desiredServiceName in ffmpegState.MetadataServiceName)
            {
                _pipelineSteps.Add(new MetadataServiceNameOutputOption(desiredServiceName));
            }

            foreach (string desiredAudioLanguage in ffmpegState.MetadataAudioLanguage)
            {
                _pipelineSteps.Add(new MetadataAudioLanguageOutputOption(desiredAudioLanguage));
            }

            switch (ffmpegState.OutputFormat)
            {
                case OutputFormatKind.MpegTs:
                    _pipelineSteps.Add(new OutputFormatMpegTs());
                    _pipelineSteps.Add(new PipeProtocol());
                    // currentState = currentState with { OutputFormat = OutputFormatKind.MpegTs };
                    break;
                case OutputFormatKind.Hls:
                    foreach (string playlistPath in ffmpegState.HlsPlaylistPath)
                    {
                        foreach (string segmentTemplate in ffmpegState.HlsSegmentTemplate)
                        {
                            var step = new OutputFormatHls(
                                desiredState,
                                videoStream.FrameRate,
                                segmentTemplate,
                                playlistPath);
                            currentState = step.NextState(currentState);
                            _pipelineSteps.Add(step);
                        }
                    }

                    break;
            }

            var complexFilter = new ComplexFilter(
                currentState,
                ffmpegState,
                _videoInputFile,
                _audioInputFile,
                _watermarkInputFile,
                _subtitleInputFile,
                currentState.PaddedSize,
                _fontsFolder);

            _pipelineSteps.Add(complexFilter);
        }

        return new FFmpegPipeline(_pipelineSteps);
    }

    private static bool IsDesiredVideoState(FrameState currentState, FrameState desiredState)
    {
        if (desiredState.VideoFormat == VideoFormat.Copy)
        {
            return true;
        }

        return currentState.VideoFormat == desiredState.VideoFormat &&
               currentState.PixelFormat.Match(pf => pf.Name, () => string.Empty) ==
               desiredState.PixelFormat.Match(pf => pf.Name, string.Empty) &&
               (desiredState.VideoBitrate.IsNone || currentState.VideoBitrate == desiredState.VideoBitrate) &&
               (desiredState.VideoBufferSize.IsNone || currentState.VideoBufferSize == desiredState.VideoBufferSize) &&
               currentState.Realtime == desiredState.Realtime &&
               (desiredState.VideoTrackTimeScale.IsNone ||
                currentState.VideoTrackTimeScale == desiredState.VideoTrackTimeScale) &&
               currentState.ScaledSize == desiredState.ScaledSize &&
               currentState.PaddedSize == desiredState.PaddedSize &&
               (desiredState.FrameRate.IsNone || currentState.FrameRate == desiredState.FrameRate);
    }
}
