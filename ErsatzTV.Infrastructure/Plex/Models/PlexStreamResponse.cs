using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexStreamResponse
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("index")]
    public int Index { get; set; }

    [XmlAttribute("default")]
    public bool Default { get; set; }

    [XmlAttribute("forced")]
    public bool Forced { get; set; }

    [XmlAttribute("languageCode")]
    public string LanguageCode { get; set; }

    [XmlAttribute("streamType")]
    public int StreamType { get; set; }

    [XmlAttribute("codec")]
    public string Codec { get; set; }

    [XmlAttribute("profile")]
    public string Profile { get; set; }

    [XmlAttribute("channels")]
    public int Channels { get; set; }

    [XmlAttribute("anamorphic")]
    public bool Anamorphic { get; set; }

    [XmlAttribute("pixelAspectRatio")]
    public string PixelAspectRatio { get; set; }

    [XmlAttribute("scanType")]
    public string ScanType { get; set; }

    [XmlAttribute("displayTitle")]
    public string DisplayTitle { get; set; }

    [XmlAttribute("extendedDisplayTitle")]
    public string ExtendedDisplayTitle { get; set; }
}
