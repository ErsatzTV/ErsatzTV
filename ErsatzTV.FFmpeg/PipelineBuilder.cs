using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.Option;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.Protocol;
using LanguageExt;

namespace ErsatzTV.FFmpeg;

public class PipelineBuilder
{
    private readonly List<IPipelineStep> _pipelineSteps;
    private readonly List<IPipelineFilterStep> _audioFilterSteps;
    private readonly List<IPipelineFilterStep> _videoFilterSteps;
    private readonly IList<InputFile> _inputFiles;

    public PipelineBuilder(IList<InputFile> inputFiles)
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
                : new NoSceneDetectOutputOption(1000000000));

        _audioFilterSteps = new List<IPipelineFilterStep>();
        _videoFilterSteps = new List<IPipelineFilterStep>();

        _inputFiles = inputFiles;
    }

    public PipelineBuilder WithRealtimeInput()
    {
        _pipelineSteps.Add(new RealtimeInputOption());
        return this;
    }

    public PipelineBuilder WithVideoTrackTimescale()
    {
        _pipelineSteps.Add(new VideoTrackTimescaleOutputOption());
        return this;
    }

    public PipelineBuilder WithAudioSampleRate(int sampleRate)
    {
        _pipelineSteps.Add(new AudioSampleRateOutputOption(sampleRate));
        return this;
    }

    public PipelineBuilder WithSlice(TimeSpan? start, TimeSpan finish)
    {
        _pipelineSteps.Add(new SliceOption(start, finish));
        return this;
    }

    public PipelineBuilder WithAudioDuration(TimeSpan audioDuration)
    {
        _audioFilterSteps.Add(new AudioPadFilter(audioDuration));
        return this;
    }

    public IList<IPipelineStep> Build(FrameState desiredState)
    {
        InputFile head = _inputFiles.First();
        var videoStream = head.Streams.First(s => s.Kind == StreamKind.Video) as VideoStream;
        var audioStream = head.Streams.First(s => s.Kind == StreamKind.Audio) as AudioStream;
        if (videoStream != null && audioStream != null)
        {
            var currentState = new FrameState(
                videoStream.Codec,
                videoStream.PixelFormat,
                Option<int>.None,
                Option<int>.None,
                audioStream.Codec,
                audioStream.Channels,
                Option<int>.None,
                Option<int>.None);

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
                    continue;
                }

                if (currentState.AudioChannels != desiredState.AudioChannels)
                {
                    var step = new AudioChannelsOutputOption(desiredState.AudioChannels);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                    continue;
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
               currentState.VideoBufferSize == desiredState.VideoBufferSize;
    }

    private static bool IsDesiredAudioState(FrameState currentState, FrameState desiredState)
    {
        return currentState.AudioFormat == desiredState.AudioFormat &&
               currentState.AudioChannels == desiredState.AudioChannels &&
               currentState.AudioBitrate == desiredState.AudioBitrate &&
               currentState.AudioBufferSize == desiredState.AudioBufferSize;
    }
}
