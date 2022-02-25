﻿using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Option;

public class InfiniteLoopInputOption : IPipelineStep
{
    private readonly HardwareAccelerationMode _hardwareAccelerationMode;

    public InfiniteLoopInputOption(HardwareAccelerationMode hardwareAccelerationMode)
    {
        _hardwareAccelerationMode = hardwareAccelerationMode;
    }

    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();

    public IList<string> InputOptions(InputFile inputFile)
    {
        // never loop audio
        if (inputFile.Streams.All(s => s.Kind == StreamKind.Video))
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
        _hardwareAccelerationMode is HardwareAccelerationMode.Qsv or HardwareAccelerationMode.Vaapi
            ? new List<string> { "-noautoscale" }
            : Array.Empty<string>();

    public FrameState NextState(FrameState currentState) => currentState with { Realtime = true };
}
