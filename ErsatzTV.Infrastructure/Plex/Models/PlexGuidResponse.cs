using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexGuidResponse
{
    [XmlAttribute("id")]
    public string Id { get; set; }
}