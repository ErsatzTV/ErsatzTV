using System.Collections.Generic;
using System.Xml.Serialization;

namespace ErsatzTV.Core.Metadata.Nfo
{
    [XmlRoot("artist")]
    public class ArtistNfo
    {
        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("disambiguation")]
        public string Disambiguation { get; set; }

        [XmlElement("genre")]
        public List<string> Genres { get; set; }

        [XmlElement("style")]
        public List<string> Styles { get; set; }

        [XmlElement("mood")]
        public List<string> Moods { get; set; }

        [XmlElement("biography")]
        public string Biography { get; set; }
    }
}
