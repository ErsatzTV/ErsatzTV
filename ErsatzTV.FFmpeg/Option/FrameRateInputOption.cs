namespace ErsatzTV.FFmpeg.Option;

public class FrameRateInputOption : IPipelineStep
{
    private readonly int _frameRate;

    public FrameRateInputOption(int frameRate)
    {
        _frameRate = frameRate;
    }

    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Unknown;
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions => new List<string> { "-r", _frameRate.ToString() };
    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => Array.Empty<string>();

    public FrameState NextState(FrameState currentState) => currentState with
    {
        FrameRate = _frameRate
    };
}
