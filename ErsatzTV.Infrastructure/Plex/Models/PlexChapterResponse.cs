using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexChapterResponse
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("index")]
    public int Index { get; set; }

    [XmlAttribute("startTimeOffset")]
    public long StartTimeOffset { get; set; }

    [XmlAttribute("endTimeOffset")]
    public long EndTimeOffset { get; set; }
}
