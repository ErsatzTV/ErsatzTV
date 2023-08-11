using System.Text.Json.Serialization;

namespace ErsatzTV.Infrastructure.Search.Models;

public abstract class BaseSearchItem
{
    public int Id { get; set; }

    public virtual string Type { get; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
    
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("sort_title")]
    public string SortTitle { get; set; }
    
    [JsonPropertyName("library_name")]
    public string LibraryName { get; set; }

    [JsonPropertyName("library_id")]
    public int LibraryId { get; set; }
    
    [JsonPropertyName("title_and_year")]
    public string TitleAndYear { get; set; }

    [JsonPropertyName("jump_letter")]
    public string JumpLetter { get; set; }
    
    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("metadata_kind")]
    public string MetadataKind { get; set; }
    
    [JsonPropertyName("language")]
    public List<string> Language { get; set; }
    
    [JsonPropertyName("minutes")]
    public int Minutes { get; set; }
    
    [JsonPropertyName("height")]
    public int Height { get; set; }
    
    [JsonPropertyName("width")]
    public int Width { get; set; }
    
    [JsonPropertyName("video_codec")]
    public string VideoCodec { get; set; }
    
    [JsonPropertyName("video_bit_depth")]
    public int VideoBitDepth { get; set; }
    
    [JsonPropertyName("video_dynamic_range")]
    public string VideoDynamicRange { get; set; }
    
    [JsonPropertyName("content_rating")]
    public List<string> ContentRating { get; set; }
    
    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; set; }
    
    [JsonPropertyName("added_date")]
    public string AddedDate { get; set; }
    
    [JsonPropertyName("plot")]
    public string Plot { get; set; }
    
    [JsonPropertyName("genre")]
    public List<string> Genre { get; set; }
    
    [JsonPropertyName("tag")]
    public List<string> Tag { get; set; }

    [JsonPropertyName("studio")]
    public List<string> Studio { get; set; }
    
    [JsonPropertyName("actor")]
    public List<string> Actor { get; set; }

    [JsonPropertyName("director")]
    public List<string> Director { get; set; }

    [JsonPropertyName("writer")]
    public List<string> Writer { get; set; }
    
    [JsonPropertyName("trakt_list")]
    public List<string> TraktList { get; set; }
}
