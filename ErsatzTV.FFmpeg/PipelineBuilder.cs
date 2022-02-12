using ErsatzTV.FFmpeg.Decoder;
using ErsatzTV.FFmpeg.Encoder;
using ErsatzTV.FFmpeg.Option;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.Protocol;

namespace ErsatzTV.FFmpeg;

public static class PipelineBuilder
{
    public static IList<IPipelineStep> GeneratePipeline(IEnumerable<InputFile> inputFiles, FrameState desiredState, TimeSpan? inPoint, TimeSpan duration)
    {
        var result = new List<IPipelineStep>
        {
            new NoStandardInputOption(),
            new HideBannerOption(),
            new NoStatsOption(),
            new LoglevelErrorOption(),
            new StandardFormatFlags(),
            new RealtimeInputOption(), // TODO: this should be configurable
            new VideoTrackTimescaleOutputOption(), // TODO: configurable?
            new NoSceneDetectOutputOption(),
            new NoDemuxDecodeDelayOutputOption(),
            new FastStartOutputOption(),
            new ClosedGopOutputOption(),
            new VideoBitrateOutputOption(2000, 2000, 4000), // TODO: configurable
            new AudioBitrateOutputOption(320, 320, 640), // TODO: configurable
            new AudioSampleRateOutputOption(48), // TODO: configurable
            new SliceOption(inPoint, duration),
        };

        InputFile head = inputFiles.First();
        var videoStream = head.Streams.First(s => s.Kind == StreamKind.Video) as VideoStream;
        var audioStream = head.Streams.First(s => s.Kind == StreamKind.Audio) as AudioStream;
        if (videoStream != null && audioStream != null)
        {
            var currentState = new FrameState(videoStream.Codec, videoStream.PixelFormat, audioStream.Codec);

            if (IsDesiredVideoState(currentState, desiredState))
            {
                result.Add(new EncoderCopyVideo());
            }
            else
            {
                IDecoder step = AvailableDecoders.ForVideoFormat(currentState);
                currentState = step.NextState(currentState);
                result.Add(step);
            }

            while (!IsDesiredVideoState(currentState, desiredState))
            {
                if (currentState.VideoFormat != desiredState.VideoFormat)
                {
                    // TODO: prioritize which codec is used (hw accel)
                    IEncoder step = AvailableEncoders.ForVideoFormat(desiredState);
                    currentState = step.NextState(currentState);
                    result.Add(step);
                }
            }

            // TODO: loop while not desired state?
            if (IsDesiredAudioState(currentState, desiredState))
            {
                result.Add(new EncoderCopyAudio());
            }
            
            while (!IsDesiredAudioState(currentState, desiredState))
            {
                if (currentState.AudioFormat != desiredState.AudioFormat)
                {
                    IEncoder step = AvailableEncoders.ForAudioFormat(desiredState);
                    currentState = step.NextState(currentState);
                    result.Add(step);
                }
            }

            result.Add(new OutputFormatMpegTs());
            result.Add(new PipeProtocol());
        }

        return result;
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
