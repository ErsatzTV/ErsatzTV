using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexTagMetadataResponse
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("filter")]
    public string Filter { get; set; }

    [XmlAttribute("tag")]
    public string Tag { get; set; }

    [XmlAttribute("tagType")]
    public int TagType { get; set; }
}
