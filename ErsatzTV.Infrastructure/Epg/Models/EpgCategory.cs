using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Epg.Models;

public class EpgCategory
{
    [XmlAttribute("lang")]
    public string Lang { get; set; }

    [XmlText]
    public string Value { get; set; }
}
