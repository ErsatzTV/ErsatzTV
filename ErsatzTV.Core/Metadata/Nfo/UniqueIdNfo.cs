using System.Xml.Serialization;

namespace ErsatzTV.Core.Metadata.Nfo;

public class UniqueIdNfo
{
    [XmlAttribute("default")]
    public bool Default { get; set; }

    [XmlAttribute("type")]
    public string Type { get; set; }

    [XmlText]
    public string Guid { get; set; }
}