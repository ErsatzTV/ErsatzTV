namespace ErsatzTV.Core.Domain;

public class Subtitle
{
    public int Id { get; set; }
    public SubtitleKind SubtitleKind { get; set; }
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
    public bool IsImage => Codec is "hdmv_pgs_subtitle" or "dvd_subtitle";
}
