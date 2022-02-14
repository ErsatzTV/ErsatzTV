﻿namespace ErsatzTV.FFmpeg.Filter;

public abstract class BaseFilter : IPipelineFilterStep
{
    public virtual IList<string> GlobalOptions => Array.Empty<string>();
    public virtual IList<string> InputOptions => Array.Empty<string>();
    public virtual IList<string> FilterOptions => Array.Empty<string>();
    public virtual IList<string> OutputOptions => Array.Empty<string>();
    public abstract FrameState NextState(FrameState currentState);

    public abstract string Filter { get; }
}
