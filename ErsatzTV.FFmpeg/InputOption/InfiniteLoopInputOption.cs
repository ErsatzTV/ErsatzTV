using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.InputOption;

public class InfiniteLoopInputOption : IInputOption
{
    private readonly HardwareAccelerationMode _hardwareAccelerationMode;

    public InfiniteLoopInputOption(HardwareAccelerationMode hardwareAccelerationMode) =>
        _hardwareAccelerationMode = hardwareAccelerationMode;

    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public string[] GlobalOptions => Array.Empty<string>();

    public string[] InputOptions(InputFile inputFile)
    {
        // loop 1 for still images
        if (inputFile.Streams.OfType<VideoStream>().Any(s => s.StillImage))
        {
            return new[] { "-loop", "1" };
        }

        // stream_loop for looped video i.e. filler
        return new[] { "-stream_loop", "-1" };
    }

    public string[] FilterOptions => Array.Empty<string>();

    public string[] OutputOptions =>
        _hardwareAccelerationMode is HardwareAccelerationMode.Qsv or HardwareAccelerationMode.Vaapi
            ? new[] { "-noautoscale" }
            : Array.Empty<string>();

    public FrameState NextState(FrameState currentState) => currentState with { Realtime = true };
    public bool AppliesTo(AudioInputFile audioInputFile) => true;
    public bool AppliesTo(VideoInputFile videoInputFile) => true;
    public bool AppliesTo(ConcatInputFile concatInputFile) => true;
}
