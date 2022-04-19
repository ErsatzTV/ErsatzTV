namespace ErsatzTV.Core.Domain;

public class Metadata
{
    public int Id { get; set; }
    public MetadataKind MetadataKind { get; set; }
    public string Title { get; set; }
    public string OriginalTitle { get; set; }
    public string SortTitle { get; set; }
    public int? Year { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public DateTime DateAdded { get; set; }
    public DateTime DateUpdated { get; set; }
    public List<Artwork> Artwork { get; set; }
    public List<Genre> Genres { get; set; }
    public List<Tag> Tags { get; set; }
    public List<Studio> Studios { get; set; }
    public List<Actor> Actors { get; set; }
    public List<MetadataGuid> Guids { get; set; }
    public List<Subtitle> Subtitles { get; set; }
}