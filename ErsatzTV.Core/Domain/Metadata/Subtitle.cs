namespace ErsatzTV.Core.Domain;

public class Subtitle
{
    public int Id { get; set; }
    public SubtitleKind SubtitleKind { get; set; }
    public string Title { get; set; }
    public int StreamIndex { get; set; }
    public string Codec { get; set; }
    public bool Default { get; set; }
    public bool Forced { get; set; }
    public bool SDH { get; set; }
    public string Language { get; set; }
    public bool IsExtracted { get; set; }
    public string Path { get; set; }
    public DateTime DateAdded { get; set; }
    public DateTime DateUpdated { get; set; }
    public bool IsImage => Codec is "hdmv_pgs_subtitle" or "dvd_subtitle" or "dvdsub" or "vobsub" or "pgssub" or "pgs";

    public static Subtitle FromMediaStream(MediaStream stream) =>
        new()
        {
            Codec = stream.Codec,
            Title = stream.Title,
            Default = stream.Default,
            Forced = stream.Forced,
            Language = stream.Language,
            StreamIndex = stream.Index,
            Path = !string.IsNullOrWhiteSpace(stream.FileName) ? stream.FileName : null,
            SubtitleKind = stream.MediaStreamKind == MediaStreamKind.ExternalSubtitle
                ? SubtitleKind.Sidecar
                : SubtitleKind.Embedded,
            DateAdded = DateTime.UtcNow,
            DateUpdated = DateTime.UtcNow
        };

    public static string ExtensionForCodec(string codec) => codec switch
    {
        "subrip" or "srt" => "srt",
        "ass" => "ass",
        "webvtt" => "vtt",
        "mov_text" => "srt",
        _ => string.Empty
    };
}
