namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record SkipItemsRequestModel
{
    public string Content { get; set; }
    public int Count { get; set; }
}
