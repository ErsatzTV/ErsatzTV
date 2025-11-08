using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.FFmpeg.Capabilities;

[SuppressMessage("ReSharper", "IdentifierTypo")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public record FFmpegKnownFormat
{
    public static readonly FFmpegKnownFormat AviSynth = new("avisynth");

    private FFmpegKnownFormat(string Name) => this.Name = Name;

    public string Name { get; }

    public static IList<string> AllFormats =>
    [
        AviSynth.Name
    ];
}
