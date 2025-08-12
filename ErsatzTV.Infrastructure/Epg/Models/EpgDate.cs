using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Epg.Models;

public class EpgDate
{
    [XmlText]
    public string Value { get; set; }
}
