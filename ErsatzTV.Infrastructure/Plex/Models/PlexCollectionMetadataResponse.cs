using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexCollectionMetadataResponse
{
    [XmlAttribute("key")]
    public string Key { get; set; }

    [XmlAttribute("ratingKey")]
    public string RatingKey { get; set; }

    [XmlAttribute("title")]
    public string Title { get; set; }

    [XmlAttribute("addedAt")]
    public long AddedAt { get; set; }

    [XmlAttribute("updatedAt")]
    public long UpdatedAt { get; set; }
    
    [XmlAttribute("smart")]
    public string Smart { get; set; }
    
    [XmlAttribute("librarySectionId")]
    public int LibrarySectionId { get; set; }
    
    [XmlAttribute("childCount")]
    public string ChildCount { get; set; }
}
