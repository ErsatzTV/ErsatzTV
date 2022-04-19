using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexXmlMetadataResponse : PlexMetadataResponse
{
    [XmlAttribute("guid")]
    public string PlexGuid { get; set; }

    [XmlElement("Media")]
    public new List<PlexMediaResponse<PlexXmlPartResponse>> Media { get; set; }

    [XmlElement("Guid")]
    public List<PlexGuidResponse> Guid { get; set; }
}
