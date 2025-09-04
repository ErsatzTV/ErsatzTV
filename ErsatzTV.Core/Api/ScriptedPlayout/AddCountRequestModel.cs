using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddCountRequestModel
{
    [Description("The 'key' for the content that should be added")]
    public string Content { get; set; }

    public int Count { get; set; }

    [Description("Flags this content as filler, which influences EPG grouping")]
    public string FillerKind { get; set; }

    [Description("Overrides the title used in the EPG")]
    public string CustomTitle { get; set; }

    public bool DisableWatermarks { get; set; }
}
