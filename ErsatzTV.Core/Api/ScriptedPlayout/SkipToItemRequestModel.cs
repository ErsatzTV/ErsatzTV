namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record SkipToItemRequestModel
{
    public string Content { get; set; }
    public int Season { get; set; }
    public int Episode { get; set; }
}
