using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Option;

public class InfiniteLoopInputOption : IPipelineStep
{
    private readonly FrameState _currentState;

    public InfiniteLoopInputOption(FrameState currentState)
    {
        _currentState = currentState;
    }

    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions(InputFile inputFile)
    {
        // only loop video media
        if (inputFile.Streams.OfType<VideoStream>().Any())
        {
            // loop 1 for still images
            if (inputFile.Streams.OfType<VideoStream>().Any(s => s.StillImage))
            {
                return new List<string> { "-loop", "1" };
            }

            // stream_loop for looped video i.e. filler
            return new List<string> { "-stream_loop", "-1" };
        }

        return Array.Empty<string>();
    }

    public IList<string> FilterOptions => Array.Empty<string>();

    public IList<string> OutputOptions =>
        _currentState.HardwareAccelerationMode is HardwareAccelerationMode.Qsv or HardwareAccelerationMode.Vaapi
            ? new List<string> { "-noautoscale" }
            : Array.Empty<string>();

    public FrameState NextState(FrameState currentState) => currentState with { Realtime = true };
}
