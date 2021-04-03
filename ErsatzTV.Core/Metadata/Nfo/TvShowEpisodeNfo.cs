using System.Xml.Serialization;

namespace ErsatzTV.Core.Metadata.Nfo
{
    [XmlRoot("episodedetails")]
    public class TvShowEpisodeNfo
    {
        [XmlElement("showtitle")]
        public string ShowTitle { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("episode")]
        public int Episode { get; set; }

        [XmlElement("season")]
        public int Season { get; set; }

        [XmlElement("mpaa")]
        public string ContentRating { get; set; }

        [XmlElement("aired")]
        public string Aired { get; set; }

        [XmlElement("plot")]
        public string Plot { get; set; }
    }
}
