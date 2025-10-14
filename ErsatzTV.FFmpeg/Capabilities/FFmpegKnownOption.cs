using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.FFmpeg.Capabilities;

[SuppressMessage("ReSharper", "IdentifierTypo")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public record FFmpegKnownOption
{
    private FFmpegKnownOption(string Name) => this.Name = Name;

    public string Name { get; }

    public static IList<string> AllOptions =>
    [
    ];
}
