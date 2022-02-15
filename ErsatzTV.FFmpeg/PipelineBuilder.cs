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

namespace ErsatzTV.FFmpeg;

public class PipelineBuilder
{
    private readonly List<IPipelineStep> _pipelineSteps;
    private readonly List<IPipelineFilterStep> _audioFilterSteps;
    private readonly List<IPipelineFilterStep> _videoFilterSteps;
    private readonly IList<InputFile> _inputFiles;
    private readonly string _reportsFolder;
    private readonly ILogger _logger;

    public PipelineBuilder(IList<InputFile> inputFiles, string reportsFolder, ILogger logger)
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
            new ClosedGopOutputOption(),
        };

        _audioFilterSteps = new List<IPipelineFilterStep>();
        _videoFilterSteps = new List<IPipelineFilterStep>();

        _inputFiles = inputFiles;
        _reportsFolder = reportsFolder;
        _logger = logger;
    }

    public IList<IPipelineStep> Build(FrameState desiredState)
    {
        var allVideoStreams = _inputFiles.SelectMany(f => f.Streams)
            .Filter(s => s.Kind == StreamKind.Video)
            .ToList();

        // -sc_threshold 0 is unsupported with mpeg2video
        _pipelineSteps.Add(
            allVideoStreams.All(s => s.Codec != VideoFormat.Mpeg2Video) && desiredState.VideoFormat != VideoFormat.Mpeg2Video
                ? new NoSceneDetectOutputOption(0)
                : new NoSceneDetectOutputOption(1_000_000_000));

        InputFile head = _inputFiles.First();
        var videoStream = head.Streams.First(s => s.Kind == StreamKind.Video) as VideoStream;
        Option<AudioStream> audioStream = head.Streams.OfType<AudioStream>().Find(s => s.Kind == StreamKind.Audio);
        if (videoStream != null)
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
                false, // save report
                HardwareAccelerationMode.None,
                Option<string>.None,
                Option<string>.None,
                false, // realtime
                false, // infinite loop
                Option<TimeSpan>.None,
                Option<TimeSpan>.None,
                videoStream.Codec,
                videoStream.PixelFormat,
                videoStream.FrameSize,
                videoStream.FrameSize,
                initialFrameRate,
                Option<int>.None,
                Option<int>.None,
                Option<int>.None,
                false, // deinterlace
                audioStream.Map(a => a.Codec),
                audioStream.Map(a => a.Channels),
                Option<int>.None,
                Option<int>.None,
                Option<int>.None,
                Option<TimeSpan>.None,
                false,
                false,
                Option<string>.None,
                Option<string>.None,
                Option<string>.None,
                OutputFormatKind.None,
                Option<string>.None,
                Option<string>.None,
                0);

            if (desiredState.SaveReport && !currentState.SaveReport)
            {
                IPipelineStep step = new FFReportVariable(_reportsFolder, _inputFiles);
                currentState = step.NextState(currentState);
                _pipelineSteps.Add(step);
            }

            foreach (TimeSpan desiredStart in desiredState.Start)
            {
                if (currentState.Start != desiredStart)
                {
                    // _logger.LogInformation("Setting stream seek: {DesiredStart}", desiredStart);
                    IPipelineStep step = new StreamSeekInputOption(desiredStart);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }
            }

            foreach (TimeSpan desiredFinish in desiredState.Finish)
            {
                if (currentState.Finish != desiredFinish)
                {
                    // _logger.LogInformation("Setting time limit: {DesiredFinish}", desiredFinish);
                    IPipelineStep step = new TimeLimitOutputOption(desiredFinish);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }
            }

            IEncoder encoder;

            if (IsDesiredVideoState(currentState, desiredState))
            {
                encoder = new EncoderCopyVideo();
                _pipelineSteps.Add(encoder);
            }
            else
            {
                if (currentState.HardwareAccelerationMode != desiredState.HardwareAccelerationMode)
                {
                    Option<IPipelineStep> maybeAccel = AvailableHardwareAccelerationOptions.ForMode(
                        desiredState.HardwareAccelerationMode,
                        desiredState.VaapiDevice,
                        _logger);

                    if (maybeAccel.IsNone)
                    {
                        desiredState = desiredState with
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
                }

                foreach (string desiredVaapiDriver in desiredState.VaapiDriver)
                {
                    if (currentState.VaapiDriver != desiredVaapiDriver)
                    {
                        IPipelineStep step = new LibvaDriverNameVariable(desiredVaapiDriver);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (IDecoder decoder in AvailableDecoders.ForVideoFormat(currentState, desiredState, _logger))
                {
                    currentState = decoder.NextState(currentState);
                    _pipelineSteps.Add(decoder);
                }

                if (_inputFiles.OfType<ConcatInputFile>().Any())
                {
                    IPipelineStep concatInputFormat = new ConcatInputFormat();
                    currentState = concatInputFormat.NextState(currentState);
                    _pipelineSteps.Add(concatInputFormat);

                    IPipelineStep copyCodec = new EncoderCopyAll();
                    currentState = copyCodec.NextState(currentState);
                    _pipelineSteps.Add(copyCodec);
                }
            }

            // TODO: while?
            if (!IsDesiredVideoState(currentState, desiredState))
            {
                if (!currentState.Realtime && desiredState.Realtime)
                {
                    IPipelineStep step = new RealtimeInputOption();
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }

                if (!currentState.InfiniteLoop && desiredState.InfiniteLoop)
                {
                    IPipelineStep step = new InfiniteLoopInputOption(currentState);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
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
                        currentState.HardwareAccelerationMode,
                        currentState);

                    currentState = step.NextState(currentState);
                    _videoFilterSteps.Add(step);
                }

                // TODO: this is a software-only flow, will need to be different for hardware accel
                if (currentState.HardwareAccelerationMode == HardwareAccelerationMode.None)
                {
                    if (currentState.ScaledSize != desiredState.ScaledSize ||
                        currentState.PaddedSize != desiredState.PaddedSize)
                    {
                        IPipelineFilterStep scaleStep = new ScaleFilter(
                            currentState,
                            desiredState.ScaledSize,
                            desiredState.PaddedSize);
                        currentState = scaleStep.NextState(currentState);
                        _videoFilterSteps.Add(scaleStep);

                        // TODO: padding might not be needed, can we optimize this out?
                        IPipelineFilterStep padStep = new PadFilter(currentState, desiredState.PaddedSize);
                        currentState = padStep.NextState(currentState);
                        _videoFilterSteps.Add(padStep);

                        IPipelineFilterStep sarStep = new SetSarFilter();
                        currentState = sarStep.NextState(currentState);
                        _videoFilterSteps.Add(sarStep);
                    }
                }
                else if (currentState.ScaledSize != desiredState.ScaledSize)
                {
                    IPipelineFilterStep scaleFilter = AvailableScaleFilters.ForAcceleration(
                        currentState.HardwareAccelerationMode,
                        currentState,
                        desiredState.ScaledSize,
                        desiredState.PaddedSize);
                    currentState = scaleFilter.NextState(currentState);
                    _videoFilterSteps.Add(scaleFilter);

                    // TODO: padding might not be needed, can we optimize this out?
                    if (currentState.PaddedSize != desiredState.PaddedSize)
                    {
                        IPipelineFilterStep padStep = new PadFilter(currentState, desiredState.PaddedSize);
                        currentState = padStep.NextState(currentState);
                        _videoFilterSteps.Add(padStep);
                    }
                    
                    IPipelineFilterStep sarStep = new SetSarFilter();
                    currentState = sarStep.NextState(currentState);
                    _videoFilterSteps.Add(sarStep);
                }
                else if (currentState.PaddedSize != desiredState.PaddedSize)
                {
                    IPipelineFilterStep scaleFilter = AvailableScaleFilters.ForAcceleration(
                        currentState.HardwareAccelerationMode,
                        currentState,
                        desiredState.ScaledSize,
                        desiredState.PaddedSize);
                    currentState = scaleFilter.NextState(currentState);
                    _videoFilterSteps.Add(scaleFilter);

                    if (currentState.PaddedSize != desiredState.PaddedSize)
                    {
                        IPipelineFilterStep padStep = new PadFilter(currentState, desiredState.PaddedSize);
                        currentState = padStep.NextState(currentState);
                        _videoFilterSteps.Add(padStep);
                    }

                    IPipelineFilterStep sarStep = new SetSarFilter();
                    currentState = sarStep.NextState(currentState);
                    _videoFilterSteps.Add(sarStep);
                }

                if (currentState.PtsOffset != desiredState.PtsOffset)
                {
                    foreach (int videoTrackTimeScale in desiredState.VideoTrackTimeScale)
                    {
                        IPipelineStep step = new OutputTsOffsetOption(
                            desiredState.PtsOffset,
                            videoTrackTimeScale);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                // after everything else is done, apply the encoder
                if (!_pipelineSteps.OfType<IEncoder>().Any())
                {
                    foreach (IEncoder e in AvailableEncoders.ForVideoFormat(currentState, desiredState, _logger))
                    {
                        encoder = e;
                        _pipelineSteps.Add(encoder);
                        _videoFilterSteps.Add(encoder);
                        currentState = encoder.NextState(currentState);
                    }
                }
            }
            
            // TODO: if all video filters are software, use software pixel format for hwaccel output
            // might be able to skip scale_cuda=format=whatever,hwdownload,format=whatever

            if (audioStream.IsSome && IsDesiredAudioState(currentState, desiredState))
            {
                _pipelineSteps.Add(new EncoderCopyAudio());
            }
            
            // TODO: while?
            if (!IsDesiredAudioState(currentState, desiredState))
            {
                if (currentState.AudioFormat != desiredState.AudioFormat)
                {
                    foreach (IEncoder step in AvailableEncoders.ForAudioFormat(desiredState, _logger))
                    {
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (int desiredAudioChannels in desiredState.AudioChannels)
                {
                    if (currentState.AudioChannels != desiredAudioChannels)
                    {
                        var step = new AudioChannelsOutputOption(desiredAudioChannels);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (int desiredBitrate in desiredState.AudioBitrate)
                {
                    if (currentState.AudioBitrate != desiredBitrate)
                    {
                        IPipelineStep step = new AudioBitrateOutputOption(desiredBitrate);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (int desiredBufferSize in desiredState.AudioBufferSize)
                {
                    if (currentState.AudioBufferSize != desiredBufferSize)
                    {
                        IPipelineStep step = new AudioBufferSizeOutputOption(desiredBufferSize);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (int desiredSampleRate in desiredState.AudioSampleRate)
                {
                    if (currentState.AudioSampleRate != desiredSampleRate)
                    {
                        IPipelineStep step = new AudioSampleRateOutputOption(desiredSampleRate);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }
                
                if (desiredState.NormalizeLoudness && !currentState.NormalizeLoudness)
                {
                    IPipelineFilterStep step = new NormalizeLoudnessFilter();
                    currentState = step.NextState(currentState);
                    _audioFilterSteps.Add(step);
                }

                foreach (TimeSpan desiredDuration in desiredState.AudioDuration)
                {
                    if (currentState.AudioDuration != desiredDuration)
                    {
                        IPipelineFilterStep step = new AudioPadFilter(desiredDuration);
                        currentState = step.NextState(currentState);
                        _audioFilterSteps.Add(step);
                    }
                }
            }

            if (desiredState.DoNotMapMetadata && !currentState.DoNotMapMetadata)
            {
                IPipelineStep step = new DoNotMapMetadataOutputOption();
                currentState = step.NextState(currentState);
                _pipelineSteps.Add(step);
            }

            foreach (string desiredServiceProvider in desiredState.MetadataServiceProvider)
            {
                if (currentState.MetadataServiceProvider != desiredServiceProvider)
                {
                    IPipelineStep step = new MetadataServiceProviderOutputOption(desiredServiceProvider);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }
            }

            foreach (string desiredServiceName in desiredState.MetadataServiceName)
            {
                if (currentState.MetadataServiceName != desiredServiceName)
                {
                    IPipelineStep step = new MetadataServiceNameOutputOption(desiredServiceName);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }
            }

            foreach (string desiredAudioLanguage in desiredState.MetadataAudioLanguage)
            {
                if (currentState.MetadataAudioLanguage != desiredAudioLanguage)
                {
                    IPipelineStep step = new MetadataAudioLanguageOutputOption(desiredAudioLanguage);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }
            }

            switch (desiredState.OutputFormat)
            {
                case OutputFormatKind.MpegTs:
                    _pipelineSteps.Add(new OutputFormatMpegTs());
                    _pipelineSteps.Add(new PipeProtocol());
                    currentState = currentState with { OutputFormat = OutputFormatKind.MpegTs };
                    break;
                case OutputFormatKind.Hls:
                    foreach (string playlistPath in desiredState.HlsPlaylistPath)
                    {
                        foreach (string segmentTemplate in desiredState.HlsSegmentTemplate)
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

            // add a complex filter unless we are concatenating
            if (!_inputFiles.OfType<ConcatInputFile>().Any())
            {
                _pipelineSteps.Add(new ComplexFilter(_inputFiles, _audioFilterSteps, _videoFilterSteps));
            }
        }

        return _pipelineSteps;
    }

    private static bool IsDesiredVideoState(FrameState currentState, FrameState desiredState)
    {
        return currentState.HardwareAccelerationMode == desiredState.HardwareAccelerationMode &&
               currentState.VideoFormat == desiredState.VideoFormat &&
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

    private static bool IsDesiredAudioState(FrameState currentState, FrameState desiredState)
    {
        return currentState.AudioFormat == desiredState.AudioFormat &&
               currentState.AudioChannels == desiredState.AudioChannels &&
               (desiredState.AudioBitrate.IsNone || currentState.AudioBitrate == desiredState.AudioBitrate) &&
               (desiredState.AudioBufferSize.IsNone || currentState.AudioBufferSize == desiredState.AudioBufferSize) &&
               (desiredState.AudioSampleRate.IsNone || currentState.AudioSampleRate == desiredState.AudioSampleRate) &&
               (desiredState.AudioDuration.IsNone || currentState.AudioDuration == desiredState.AudioDuration) &&
               currentState.NormalizeLoudness == desiredState.NormalizeLoudness;
    }
}
