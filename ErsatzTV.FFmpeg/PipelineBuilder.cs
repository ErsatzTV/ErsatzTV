using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Option;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.Protocol;

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
            new NoSceneDetectOutputOption(),
            new NoDemuxDecodeDelayOutputOption(),
            new FastStartOutputOption(),
            new ClosedGopOutputOption(),
        };

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

    public PipelineBuilder WithVideoBitrate(int averageBitrate, int maximumTolerance, int decoderBufferSize)
    {
        _pipelineSteps.Add(new VideoBitrateOutputOption(averageBitrate, maximumTolerance, decoderBufferSize));
        return this;
    }
    
    public PipelineBuilder WithAudioBitrate(int averageBitrate, int maximumTolerance, int decoderBufferSize)
    {
        _pipelineSteps.Add(new AudioBitrateOutputOption(averageBitrate, maximumTolerance, decoderBufferSize));
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
                audioStream.Codec,
                audioStream.Channels);

            if (IsDesiredVideoState(currentState, desiredState))
            {
                _pipelineSteps.Add(new EncoderCopyVideo());
            }
            else
            {
                IDecoder step = AvailableDecoders.ForVideoFormat(currentState);
                currentState = step.NextState(currentState);
                _pipelineSteps.Add(step);
            }

            while (!IsDesiredVideoState(currentState, desiredState))
            {
                if (currentState.VideoFormat != desiredState.VideoFormat)
                {
                    // TODO: prioritize which codec is used (hw accel)
                    IEncoder step = AvailableEncoders.ForVideoFormat(desiredState);
                    currentState = step.NextState(currentState);
                    _pipelineSteps.Add(step);
                }
            }

            // TODO: loop while not desired state?
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
               currentState.PixelFormat.Name == desiredState.PixelFormat.Name;
    }

    private static bool IsDesiredAudioState(FrameState currentState, FrameState desiredState)
    {
        return currentState.AudioFormat == desiredState.AudioFormat;
    }
}
