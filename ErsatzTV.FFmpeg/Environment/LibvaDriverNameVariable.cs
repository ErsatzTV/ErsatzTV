namespace ErsatzTV.FFmpeg.Environment;

public class LibvaDriverNameVariable : IPipelineStep
{
    private readonly string _driverName;

    public LibvaDriverNameVariable(string driverName)
    {
        _driverName = driverName;
    }

    public IList<EnvironmentVariable> EnvironmentVariables => new List<EnvironmentVariable>
    {
        new("LIBVA_DRIVER_NAME", _driverName)
    };

    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();
    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;
}
