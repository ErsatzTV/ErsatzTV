namespace ErsatzTV.FFmpeg.Option;

public class FrameRateOutputOption : IPipelineStep
{
    private readonly int _frameRate;

    public FrameRateOutputOption(int frameRate)
    {
        _frameRate = frameRate;
    }

    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Unknown;
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions => Array.Empty<string>();
    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => new List<string> { "-r", _frameRate.ToString(), "-vsync", "cfr" };

    public FrameState NextState(FrameState currentState) => currentState with
    {
        FrameRate = _frameRate
    };
}
