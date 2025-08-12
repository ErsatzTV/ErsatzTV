using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Epg.Models;

public class EpgEpisodeNum
{
    [XmlAttribute("system")]
    public string System { get; set; }

    [XmlText]
    public string Value { get; set; }
}
