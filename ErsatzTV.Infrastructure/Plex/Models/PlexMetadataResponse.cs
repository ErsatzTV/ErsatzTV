using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexMetadataResponse
{
    [XmlAttribute("key")]
    public string Key { get; set; }

    [XmlAttribute("title")]
    public string Title { get; set; }

    [XmlAttribute("contentRating")]
    public string ContentRating { get; set; }

    [XmlAttribute("summary")]
    public string Summary { get; set; }

    [XmlAttribute("year")]
    public int Year { get; set; }

    [XmlAttribute("tagline")]
    public string Tagline { get; set; }

    [XmlAttribute("thumb")]
    public string Thumb { get; set; }

    [XmlAttribute("art")]
    public string Art { get; set; }

    [XmlAttribute("originallyAvailableAt")]
    public string OriginallyAvailableAt { get; set; }

    [XmlAttribute("addedAt")]
    public long AddedAt { get; set; }

    [XmlAttribute("updatedAt")]
    public long UpdatedAt { get; set; }

    [XmlAttribute("index")]
    public int Index { get; set; }

    [XmlAttribute("studio")]
    public string Studio { get; set; }

    [XmlAttribute("rating")]
    public double Rating { get; set; }

    [XmlAttribute("audienceRating")]
    public double AudienceRating { get; set; }

    [XmlAttribute("audienceRatingImage")]
    public string AudienceRatingImage { get; set; }

    [XmlAttribute("ratingImage")]
    public string RatingImage { get; set; }

    [XmlIgnore]
    public virtual List<PlexMediaResponse<PlexPartResponse>> Media { get; set; }

    [XmlElement("Genre")]
    public List<PlexGenreResponse> Genre { get; set; }

    [XmlElement("Label")]
    public List<PlexLabelResponse> Label { get; set; }

    [XmlElement("Role")]
    public List<PlexRoleResponse> Role { get; set; }

    [XmlElement("Director")]
    public List<PlexDirectorResponse> Director { get; set; }

    [XmlElement("Writer")]
    public List<PlexWriterResponse> Writer { get; set; }

    [XmlElement("Collection")]
    public List<PlexCollectionResponse> Collection { get; set; }

    [XmlElement("Chapter")]
    public List<PlexChapterResponse> Chapters { get; set; }
}
