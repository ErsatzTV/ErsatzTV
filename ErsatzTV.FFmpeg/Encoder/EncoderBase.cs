using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Encoder;

public abstract class EncoderBase : IEncoder
{
    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public string[] GlobalOptions => Array.Empty<string>();
    public string[] InputOptions(InputFile inputFile) => Array.Empty<string>();
    public string[] FilterOptions => Array.Empty<string>();

    public virtual string[] OutputOptions => new[]
    {
        Kind switch
        {
            StreamKind.Video => "-c:v",
            StreamKind.Audio => "-c:a",
            StreamKind.Subtitle => "-c:s",
            _ => throw new ArgumentOutOfRangeException()
        },
        Name
    };

    public virtual FrameState NextState(FrameState currentState) => currentState;

    public abstract string Name { get; }
    public abstract StreamKind Kind { get; }
    public virtual string Filter => string.Empty;
}
