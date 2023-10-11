using System.Text.Json.Serialization;

namespace ErsatzTV.Infrastructure.Search.Models;

public class ElasticSearchItem : MinimalElasticSearchItem
{
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();

    [JsonPropertyName(LuceneSearchIndex.TitleField)]
    public string Title { get; set; }

    [JsonPropertyName(LuceneSearchIndex.LibraryNameField)]
    public string LibraryName { get; set; }

    [JsonPropertyName(LuceneSearchIndex.LibraryIdField)]
    public int LibraryId { get; set; }

    [JsonPropertyName(LuceneSearchIndex.TitleAndYearField)]
    public string TitleAndYear { get; set; }

    [JsonPropertyName(LuceneSearchIndex.StateField)]
    public string State { get; set; }

    [JsonPropertyName(LuceneSearchIndex.MetadataKindField)]
    public string MetadataKind { get; set; }

    [JsonPropertyName(LuceneSearchIndex.LanguageField)]
    public List<string> Language { get; set; }
    
    [JsonPropertyName(LuceneSearchIndex.LanguageTagField)]
    public List<string> LanguageTag { get; set; }

    [JsonPropertyName(LuceneSearchIndex.MinutesField)]
    public int Minutes { get; set; }
    
    [JsonPropertyName(LuceneSearchIndex.SecondsField)]
    public int Seconds { get; set; }

    [JsonPropertyName(LuceneSearchIndex.HeightField)]
    public int Height { get; set; }

    [JsonPropertyName(LuceneSearchIndex.WidthField)]
    public int Width { get; set; }

    [JsonPropertyName(LuceneSearchIndex.VideoCodecField)]
    public string VideoCodec { get; set; }

    [JsonPropertyName(LuceneSearchIndex.VideoBitDepthField)]
    public int VideoBitDepth { get; set; }

    [JsonPropertyName(LuceneSearchIndex.VideoDynamicRange)]
    public string VideoDynamicRange { get; set; }

    [JsonPropertyName(LuceneSearchIndex.ContentRatingField)]
    public List<string> ContentRating { get; set; }

    [JsonPropertyName(LuceneSearchIndex.ReleaseDateField)]
    public string ReleaseDate { get; set; }

    [JsonPropertyName(LuceneSearchIndex.AddedDateField)]
    public string AddedDate { get; set; }

    [JsonPropertyName(LuceneSearchIndex.AlbumField)]
    public string Album { get; set; }

    [JsonPropertyName(LuceneSearchIndex.AlbumArtistField)]
    public string AlbumArtist { get; set; }

    [JsonPropertyName(LuceneSearchIndex.PlotField)]
    public string Plot { get; set; }

    [JsonPropertyName(LuceneSearchIndex.GenreField)]
    public List<string> Genre { get; set; }

    [JsonPropertyName(LuceneSearchIndex.TagField)]
    public List<string> Tag { get; set; }

    [JsonPropertyName(LuceneSearchIndex.StudioField)]
    public List<string> Studio { get; set; }

    [JsonPropertyName(LuceneSearchIndex.ArtistField)]
    public List<string> Artist { get; set; }

    [JsonPropertyName(LuceneSearchIndex.ActorField)]
    public List<string> Actor { get; set; }

    [JsonPropertyName(LuceneSearchIndex.DirectorField)]
    public List<string> Director { get; set; }

    [JsonPropertyName(LuceneSearchIndex.WriterField)]
    public List<string> Writer { get; set; }

    [JsonPropertyName(LuceneSearchIndex.TraktListField)]
    public List<string> TraktList { get; set; }

    [JsonPropertyName(LuceneSearchIndex.SeasonNumberField)]
    public int SeasonNumber { get; set; }

    [JsonPropertyName(LuceneSearchIndex.EpisodeNumberField)]
    public int EpisodeNumber { get; set; }

    [JsonPropertyName(LuceneSearchIndex.ShowTitleField)]
    public string ShowTitle { get; set; }

    [JsonPropertyName(LuceneSearchIndex.ShowGenreField)]
    public List<string> ShowGenre { get; set; }

    [JsonPropertyName(LuceneSearchIndex.ShowTagField)]
    public List<string> ShowTag { get; set; }

    [JsonPropertyName(LuceneSearchIndex.StyleField)]
    public List<string> Style { get; set; }

    [JsonPropertyName(LuceneSearchIndex.MoodField)]
    public List<string> Mood { get; set; }
}
