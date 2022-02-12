namespace ErsatzTV.FFmpeg.Option;

public class NoSceneDetectOutputOption : OutputOption
{
    private readonly int _value;

    public NoSceneDetectOutputOption(int value)
    {
        _value = value;
    }

    public override IList<string> OutputOptions => new List<string> { "-sc_threshold", _value.ToString() };
}
