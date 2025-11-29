using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ControlSkipItems
{
    [Description("The 'key' for the content")]
    public required string Content { get; set; }

    [Description("The number of items to skip")]
    public required int Count { get; set; }
}
