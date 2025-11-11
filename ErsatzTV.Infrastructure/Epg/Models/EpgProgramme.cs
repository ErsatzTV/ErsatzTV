using System.Xml;
using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Epg.Models;

[XmlRoot("programme")]
public class EpgProgramme
{
    [XmlAttribute("start")]
    public string Start { get; set; }

    [XmlAttribute("stop")]
    public string Stop { get; set; }

    [XmlAttribute("channel")]
    public string Channel { get; set; }

    [XmlElement("title")]
    public EpgTitle Title { get; set; }

    [XmlElement("sub-title")]
    public EpgTitle SubTitle { get; set; }

    [XmlElement("desc")]
    public EpgDescription Description { get; set; }

    [XmlElement("category")]
    public List<EpgCategory> Categories { get; set; }

    [XmlElement("icon")]
    public EpgIcon Icon { get; set; }

    [XmlElement("episode-num")]
    public List<EpgEpisodeNum> EpisodeNums { get; set; }

    [XmlElement("rating")]
    public EpgRating Rating { get; set; }

    [XmlElement("previously-shown")]
    public object PreviouslyShown { get; set; }

    [XmlElement("date")]
    public EpgDate Date { get; set; }

    [XmlAnyElement]
    public XmlElement[] OtherElements { get; set; }
}
