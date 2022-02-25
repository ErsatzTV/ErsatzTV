using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.Option;
using ErsatzTV.FFmpeg.Option.HardwareAcceleration;
using ErsatzTV.FFmpeg.Option.Metadata;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.Protocol;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.FFmpeg;

public class PipelineBuilder
{
    private readonly List<IPipelineStep> _pipelineSteps;
    private readonly Option<VideoInputFile> _videoInputFile;
    private readonly Option<AudioInputFile> _audioInputFile;
    private readonly string _reportsFolder;
    private readonly ILogger _logger;

    public PipelineBuilder(
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        string reportsFolder,
        ILogger logger)
    {
        _pipelineSteps = new List<IPipelineStep>
        {
            new ThreadCountOption(1), // try everything single-threaded
            new NoStandardInputOption(),
            new HideBannerOption(),
            new NoStatsOption(),
            new LoglevelErrorOption(),
            new StandardFormatFlags(),
            new NoDemuxDecodeDelayOutputOption(),
            new FastStartOutputOption(),
            new ClosedGopOutputOption(),
        };

        _videoInputFile = videoInputFile;
        _audioInputFile = audioInputFile;
        _reportsFolder = reportsFolder;
        _logger = logger;
    }

    public FFmpegPipeline Concat(ConcatInputFile concatInputFile, FFmpegState ffmpegState)
    {
        concatInputFile.AddOption(new ConcatInputFormat());
        concatInputFile.AddOption(new RealtimeInputOption());
        concatInputFile.AddOption(new InfiniteLoopInputOption(HardwareAccelerationMode.None));

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
        var allVideoStreams = _videoInputFile.SelectMany(f => f.VideoStreams).ToList();

        // -sc_threshold 0 is unsupported with mpeg2video
        _pipelineSteps.Add(
            allVideoStreams.All(s => s.Codec != VideoFormat.Mpeg2Video) && desiredState.VideoFormat != VideoFormat.Mpeg2Video
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
        }

        foreach (TimeSpan desiredFinish in ffmpegState.Finish)
        {
            _pipelineSteps.Add(new TimeLimitOutputOption(desiredFinish));
        }

        foreach (VideoStream videoStream in allVideoStreams)
        {
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
                    ffmpegState.HardwareAccelerationMode,
                    ffmpegState.VaapiDevice,
                    _logger);

                if (maybeAccel.IsNone)
                {
                    ffmpegState = ffmpegState with
                    {
                        // disable hw accel if we don't match anything
                        HardwareAccelerationMode = HardwareAccelerationMode.None
                    };
                }

                foreach (IPipelineStep accel in maybeAccel)
                {
                    currentState = accel.NextState(currentState);
                    _pipelineSteps.Add(accel);
                }

                foreach (string desiredVaapiDriver in ffmpegState.VaapiDriver)
                {
                    IPipelineStep step = new LibvaDriverNameVariable(desiredVaapiDriver);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }

                foreach (IDecoder decoder in AvailableDecoders.ForVideoFormat(ffmpegState, currentState, _logger))
                {
                    foreach (VideoInputFile videoInputFile in _videoInputFile)
                    {
                        videoInputFile.AddOption(decoder);
                        currentState = decoder.NextState(currentState);
                    }
                }
            }

            if (videoStream.StillImage)
            {
                var option = new InfiniteLoopInputOption(ffmpegState.HardwareAccelerationMode);
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
                    var option = new InfiniteLoopInputOption(ffmpegState.HardwareAccelerationMode);
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
                        ffmpegState.HardwareAccelerationMode,
                        currentState);
                    currentState = step.NextState(currentState);
                    _videoInputFile.Iter(f => f.FilterSteps.Add(step));
                }

                // TODO: this is a software-only flow, will need to be different for hardware accel
                if (ffmpegState.HardwareAccelerationMode == HardwareAccelerationMode.None)
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

                        IPipelineFilterStep sarStep = new SetSarFilter();
                        currentState = sarStep.NextState(currentState);
                        _videoInputFile.Iter(f => f.FilterSteps.Add(sarStep));
                    }
                }
                else if (currentState.ScaledSize != desiredState.ScaledSize)
                {
                    IPipelineFilterStep scaleFilter = AvailableScaleFilters.ForAcceleration(
                        ffmpegState.HardwareAccelerationMode,
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
                    
                    IPipelineFilterStep sarStep = new SetSarFilter();
                    currentState = sarStep.NextState(currentState);
                    _videoInputFile.Iter(f => f.FilterSteps.Add(sarStep));
                }
                else if (currentState.PaddedSize != desiredState.PaddedSize)
                {
                    IPipelineFilterStep scaleFilter = AvailableScaleFilters.ForAcceleration(
                        ffmpegState.HardwareAccelerationMode,
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

                    IPipelineFilterStep sarStep = new SetSarFilter();
                    currentState = sarStep.NextState(currentState);
                    _videoInputFile.Iter(f => f.FilterSteps.Add(sarStep));
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
                        IPipelineStep step = new PixelFormatOutputOption(desiredPixelFormat);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                // after everything else is done, apply the encoder
                if (!_pipelineSteps.OfType<IEncoder>().Any())
                {
                    foreach (IEncoder e in AvailableEncoders.ForVideoFormat(
                                 ffmpegState,
                                 currentState,
                                 desiredState,
                                 _logger))
                    {
                        encoder = e;
                        _pipelineSteps.Add(encoder);
                        _videoInputFile.Iter(f => f.FilterSteps.Add(encoder));
                        currentState = encoder.NextState(currentState);
                    }
                }
            }
            
            // TODO: if all video filters are software, use software pixel format for hwaccel output
            // might be able to skip scale_cuda=format=whatever,hwdownload,format=whatever

            foreach (AudioInputFile audioInputFile in _audioInputFile)
            {
                // always need to specify audio codec so ffmpeg doesn't default to a codec we don't want
                foreach (IEncoder step in AvailableEncoders.ForAudioFormat(audioInputFile.DesiredState, _logger))
                {
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }

                foreach (int desiredAudioChannels in audioInputFile.DesiredState.AudioChannels)
                {
                    _pipelineSteps.Add(new AudioChannelsOutputOption(desiredAudioChannels));
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

            _pipelineSteps.Add(new ComplexFilter(_videoInputFile, _audioInputFile));
        }

        return new FFmpegPipeline(_pipelineSteps);
    }

    private static bool IsDesiredVideoState(FrameState currentState, FrameState desiredState)
    {
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
