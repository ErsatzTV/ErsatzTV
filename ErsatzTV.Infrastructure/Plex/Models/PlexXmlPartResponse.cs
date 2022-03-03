using System.Collections.Generic;
using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexXmlPartResponse
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("key")]
    public string Key { get; set; }

    [XmlAttribute("duration")]
    public int Duration { get; set; }

    [XmlAttribute("file")]
    public string File { get; set; }

    [XmlElement("Stream")]
    public List<PlexStreamResponse> Stream { get; set; }
}