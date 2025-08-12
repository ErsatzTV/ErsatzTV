using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Epg.Models;

public class EpgIcon
{
    [XmlAttribute("src")]
    public string Src { get; set; }
}
