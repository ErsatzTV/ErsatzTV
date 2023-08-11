using System.Text.Json.Serialization;

namespace ErsatzTV.Infrastructure.Search.Models;

public abstract class BaseSearchItem
{
    [JsonPropertyName(SearchIndex.IdField)]
    public int Id { get; set; }

    public virtual string Type { get; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
    
    [JsonPropertyName(SearchIndex.TitleField)]
    public string Title { get; set; }

    [JsonPropertyName(SearchIndex.SortTitleField)]
    public string SortTitle { get; set; }
    
    [JsonPropertyName(SearchIndex.LibraryNameField)]
    public string LibraryName { get; set; }

    [JsonPropertyName(SearchIndex.LibraryIdField)]
    public int LibraryId { get; set; }
    
    [JsonPropertyName(SearchIndex.TitleAndYearField)]
    public string TitleAndYear { get; set; }

    [JsonPropertyName(SearchIndex.JumpLetterField)]
    public string JumpLetter { get; set; }
    
    [JsonPropertyName(SearchIndex.StateField)]
    public string State { get; set; }

    [JsonPropertyName(SearchIndex.MetadataKindField)]
    public string MetadataKind { get; set; }
    
    [JsonPropertyName(SearchIndex.LanguageField)]
    public List<string> Language { get; set; }
    
    [JsonPropertyName(SearchIndex.MinutesField)]
    public int Minutes { get; set; }
    
    [JsonPropertyName(SearchIndex.HeightField)]
    public int Height { get; set; }
    
    [JsonPropertyName(SearchIndex.WidthField)]
    public int Width { get; set; }
    
    [JsonPropertyName(SearchIndex.VideoCodecField)]
    public string VideoCodec { get; set; }
    
    [JsonPropertyName(SearchIndex.VideoBitDepthField)]
    public int VideoBitDepth { get; set; }
    
    [JsonPropertyName(SearchIndex.VideoDynamicRange)]
    public string VideoDynamicRange { get; set; }
    
    [JsonPropertyName(SearchIndex.ContentRatingField)]
    public List<string> ContentRating { get; set; }
    
    [JsonPropertyName(SearchIndex.ReleaseDateField)]
    public string ReleaseDate { get; set; }
    
    [JsonPropertyName(SearchIndex.AddedDateField)]
    public string AddedDate { get; set; }
    
    [JsonPropertyName(SearchIndex.PlotField)]
    public string Plot { get; set; }
    
    [JsonPropertyName(SearchIndex.GenreField)]
    public List<string> Genre { get; set; }
    
    [JsonPropertyName(SearchIndex.TagField)]
    public List<string> Tag { get; set; }

    [JsonPropertyName(SearchIndex.StudioField)]
    public List<string> Studio { get; set; }
    
    [JsonPropertyName(SearchIndex.ActorField)]
    public List<string> Actor { get; set; }

    [JsonPropertyName(SearchIndex.DirectorField)]
    public List<string> Director { get; set; }

    [JsonPropertyName(SearchIndex.WriterField)]
    public List<string> Writer { get; set; }
    
    [JsonPropertyName(SearchIndex.TraktListField)]
    public List<string> TraktList { get; set; }
}
