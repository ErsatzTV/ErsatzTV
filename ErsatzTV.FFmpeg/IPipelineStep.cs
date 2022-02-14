﻿namespace ErsatzTV.FFmpeg;

public interface IPipelineStep
{
    IList<string> GlobalOptions { get; }
    IList<string> InputOptions { get; }
    IList<string> FilterOptions { get; }
    IList<string> OutputOptions { get; }

    FrameState NextState(FrameState currentState);
}
