using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record PlayoutPadUntil
{
    [Description("The 'key' for the content that should be added")]
    public string Content { get; set; }

    [Description("The time of day that content should be added until")]
    public string When { get; set; }

    [Description(
        "Only used when the current playout time is already after the specified pad until time. When true, content will be scheduled until the specified time of day (the next day). When false, no content will be scheduled by this request.")]
    public bool Tomorrow { get; set; }

    [Description(
        "The 'key' for the content that should be used to fill any remaining unscheduled time. One item will be selected to be looped and trimmed to exactly fit.")]
    public string Fallback { get; set; }

    [Description("Controls whether content will be trimmed to exactly fit until the specified time")]
    public bool Trim { get; set; }

    [Description(
        "When trim is false, this is the number of times to discard items from the collection to find something that fits until the specified time")]
    public int DiscardAttempts { get; set; }

    [Description(
        "When false, allows content to run over the specified the specified time before completing this request")]
    public bool StopBeforeEnd { get; set; } = true;

    [Description(
        "When true, afer scheduling everything that will fit, any remaining time from the specified interval will be unscheduled (offline)")]
    public bool OfflineTail { get; set; }

    [Description("Flags this content as filler, which influences EPG grouping")]
    public string FillerKind { get; set; }

    [Description("Overrides the title used in the EPG")]
    public string CustomTitle { get; set; }

    public bool DisableWatermarks { get; set; }
}
