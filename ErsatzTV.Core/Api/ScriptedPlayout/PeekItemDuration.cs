using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public class PeekItemDuration
{
    public string Content { get; set; }

    [Description("Duration in milliseconds")]
    public long Milliseconds { get; set; }
}
