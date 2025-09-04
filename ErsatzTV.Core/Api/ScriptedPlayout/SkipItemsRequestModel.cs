using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record SkipItemsRequestModel
{
    [Description("The 'key' for the content")]
    public string Content { get; set; }

    [Description("The number of items to skip")]
    public int Count { get; set; }
}
