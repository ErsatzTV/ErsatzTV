using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.Option;
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
    private readonly ILogger _logger;

    public PipelineBuilder(IList<InputFile> inputFiles, ILogger logger)
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

        var allVideoStreams = inputFiles.SelectMany(f => f.Streams)
            .Filter(s => s.Kind == StreamKind.Video)
            .ToList();

        // -sc_threshold 0 is unsupported with mpeg2video
        _pipelineSteps.Add(
            allVideoStreams.All(s => s.Codec != VideoFormat.Mpeg2Video)
                ? new NoSceneDetectOutputOption(0)
                : new NoSceneDetectOutputOption(1_000_000_000));

        _audioFilterSteps = new List<IPipelineFilterStep>();
        _videoFilterSteps = new List<IPipelineFilterStep>();

        _inputFiles = inputFiles;
        _logger = logger;
    }

    public IList<IPipelineStep> Build(FrameState desiredState)
    {
        InputFile head = _inputFiles.First();
        var videoStream = head.Streams.First(s => s.Kind == StreamKind.Video) as VideoStream;
        var audioStream = head.Streams.First(s => s.Kind == StreamKind.Audio) as AudioStream;
        if (videoStream != null && audioStream != null)
        {
            var currentState = new FrameState(
                false, // realtime
                Option<TimeSpan>.None,
                Option<TimeSpan>.None,
                videoStream.Codec,
                videoStream.PixelFormat,
                Option<int>.None,
                Option<int>.None,
                Option<int>.None,
                audioStream.Codec,
                audioStream.Channels,
                Option<int>.None,
                Option<int>.None,
                Option<int>.None,
                Option<TimeSpan>.None);

            foreach (TimeSpan desiredStart in desiredState.Start)
            {
                TimeSpan currentStart = currentState.Start.IfNone(TimeSpan.Zero);
                if (currentStart != desiredStart)
                {
                    // _logger.LogInformation("Setting stream seek: {DesiredStart}", desiredStart);
                    IPipelineStep step = new StreamSeekInputOption(desiredStart);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }
            }

            foreach (TimeSpan desiredFinish in desiredState.Finish)
            {
                TimeSpan currentFinish = currentState.Finish.IfNone(TimeSpan.Zero);
                if (currentFinish != desiredFinish)
                {
                    // _logger.LogInformation("Setting time limit: {DesiredFinish}", desiredFinish);
                    IPipelineStep step = new TimeLimitOutputOption(desiredFinish);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }
            }

            if (IsDesiredVideoState(currentState, desiredState))
            {
                _pipelineSteps.Add(new EncoderCopyVideo());
            }
            else
            {
                // TODO: prioritize which codecs are used (hw accel)

                IDecoder decoder = AvailableDecoders.ForVideoFormat(currentState);
                currentState = decoder.NextState(currentState);
                _pipelineSteps.Add(decoder);
                
                IEncoder encoder = AvailableEncoders.ForVideoFormat(desiredState);
                currentState = encoder.NextState(currentState);
                _pipelineSteps.Add(encoder);
            }

            while (!IsDesiredVideoState(currentState, desiredState))
            {
                if (!currentState.Realtime && desiredState.Realtime)
                {
                    IPipelineStep step = new RealtimeInputOption();
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }

                foreach (int desiredTimeScale in desiredState.VideoTrackTimeScale)
                {
                    int currentTimeScale = currentState.VideoTrackTimeScale.IfNone(0);
                    if (currentTimeScale != desiredTimeScale)
                    {
                        IPipelineStep step = new VideoTrackTimescaleOutputOption(desiredTimeScale);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (int desiredBitrate in desiredState.VideoBitrate)
                {
                    int currentBitrate = currentState.VideoBitrate.IfNone(0);
                    if (currentBitrate != desiredBitrate)
                    {
                        IPipelineStep step = new VideoBitrateOutputOption(desiredBitrate);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (int desiredBufferSize in desiredState.VideoBufferSize)
                {
                    int currentBufferSize = currentState.VideoBitrate.IfNone(0);
                    if (currentBufferSize != desiredBufferSize)
                    {
                        IPipelineStep step = new VideoBufferSizeOutputOption(desiredBufferSize);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }
            }

            if (IsDesiredAudioState(currentState, desiredState))
            {
                _pipelineSteps.Add(new EncoderCopyAudio());
            }
            
            while (!IsDesiredAudioState(currentState, desiredState))
            {
                if (currentState.AudioFormat != desiredState.AudioFormat)
                {
                    IEncoder step = AvailableEncoders.ForAudioFormat(desiredState);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }

                if (currentState.AudioChannels != desiredState.AudioChannels)
                {
                    var step = new AudioChannelsOutputOption(desiredState.AudioChannels);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }
                
                foreach (int desiredBitrate in desiredState.AudioBitrate)
                {
                    int currentBitrate = currentState.AudioBitrate.IfNone(0);
                    if (currentBitrate != desiredBitrate)
                    {
                        IPipelineStep step = new AudioBitrateOutputOption(desiredBitrate);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (int desiredBufferSize in desiredState.AudioBufferSize)
                {
                    int currentBufferSize = currentState.AudioBufferSize.IfNone(0);
                    if (currentBufferSize != desiredBufferSize)
                    {
                        IPipelineStep step = new AudioBufferSizeOutputOption(desiredBufferSize);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (int desiredSampleRate in desiredState.AudioSampleRate)
                {
                    int currentSampleRate = currentState.AudioSampleRate.IfNone(0);
                    if (currentSampleRate != desiredSampleRate)
                    {
                        IPipelineStep step = new AudioSampleRateOutputOption(desiredSampleRate);
                        currentState = step.NextState(currentState);
                        _pipelineSteps.Add(step);
                    }
                }

                foreach (TimeSpan desiredDuration in desiredState.AudioDuration)
                {
                    TimeSpan currentDuration = currentState.AudioDuration.IfNone(TimeSpan.Zero);
                    if (currentDuration != desiredDuration)
                    {
                        IPipelineFilterStep step = new AudioPadFilter(desiredDuration);
                        currentState = step.NextState(currentState);
                        _audioFilterSteps.Add(step);
                    }
                }
            }

            _pipelineSteps.Add(new OutputFormatMpegTs());
            _pipelineSteps.Add(new PipeProtocol());
            _pipelineSteps.Add(new ComplexFilter(_inputFiles, _audioFilterSteps, _videoFilterSteps));
        }

        return _pipelineSteps;
    }

    private static bool IsDesiredVideoState(FrameState currentState, FrameState desiredState)
    {
        return currentState.VideoFormat == desiredState.VideoFormat &&
               currentState.PixelFormat.Name == desiredState.PixelFormat.Name &&
               currentState.VideoBitrate == desiredState.VideoBitrate &&
               currentState.VideoBufferSize == desiredState.VideoBufferSize &&
               currentState.Realtime == desiredState.Realtime &&
               currentState.VideoTrackTimeScale == desiredState.VideoTrackTimeScale;
    }

    private static bool IsDesiredAudioState(FrameState currentState, FrameState desiredState)
    {
        return currentState.AudioFormat == desiredState.AudioFormat &&
               currentState.AudioChannels == desiredState.AudioChannels &&
               currentState.AudioBitrate == desiredState.AudioBitrate &&
               currentState.AudioBufferSize == desiredState.AudioBufferSize &&
               currentState.AudioSampleRate == desiredState.AudioSampleRate &&
               currentState.AudioDuration == desiredState.AudioDuration;
    }
}
