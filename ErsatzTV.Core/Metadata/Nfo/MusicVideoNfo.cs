using System.Xml.Serialization;

namespace ErsatzTV.Core.Metadata.Nfo;

[XmlRoot("musicvideo")]
public class MusicVideoNfo
{
    [XmlElement("artist")]
    public string Artist { get; set; }

    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("album")]
    public string Album { get; set; }

    [XmlElement("plot")]
    public string Plot { get; set; }

    [XmlElement("year")]
    public int Year { get; set; }

    [XmlElement("genre")]
    public List<string> Genres { get; set; }

    [XmlElement("tag")]
    public List<string> Tags { get; set; }

    [XmlElement("studio")]
    public List<string> Studios { get; set; }
}
