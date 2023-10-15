using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.FFmpeg.Capabilities;

[SuppressMessage("ReSharper", "IdentifierTypo")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public record FFmpegKnownOption
{
    public string Name { get; }

    private FFmpegKnownOption(string Name)
    {
        this.Name = Name;
    }

    public static readonly FFmpegKnownOption ReadrateInitialBurst = new("readrate_initial_burst");

    public static IList<string> AllOptions =>
        new[]
        {
            ReadrateInitialBurst.Name
        };
}
