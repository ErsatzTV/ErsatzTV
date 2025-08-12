using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Epg.Models;

public class EpgRating
{
    [XmlElement("value")]
    public string Value { get; set; }
}
