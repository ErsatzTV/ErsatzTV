namespace ErsatzTV.FFmpeg.Environment;

public class LibvaDriverNameVariable : IPipelineStep
{
    private readonly string _driverName;

    public LibvaDriverNameVariable(string driverName) => _driverName = driverName;

    public EnvironmentVariable[] EnvironmentVariables => new[]
    {
        new EnvironmentVariable("LIBVA_DRIVER_NAME", _driverName)
    };

    public string[] GlobalOptions => Array.Empty<string>();
    public string[] InputOptions(InputFile inputFile) => Array.Empty<string>();
    public string[] FilterOptions => Array.Empty<string>();
    public string[] OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;
}
