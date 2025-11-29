using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public class PeekItemDuration
{
    public required string Content { get; set; }

    [Description("Duration in milliseconds")]
    public required long Milliseconds { get; set; }
}
