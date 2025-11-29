using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record PlayoutPadUntilExact
{
    [Description("The 'key' for the content that should be added")]
    public required string Content { get; set; }

    [Description("The time content should be added until")]
    public required DateTimeOffset When { get; set; }

    [Description(
        "The 'key' for the content that should be used to fill any remaining unscheduled time. One item will be selected to be looped and trimmed to exactly fit.")]
    public string? Fallback { get; set; }

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
    public string? FillerKind { get; set; }

    [Description("Overrides the title used in the EPG")]
    public string? CustomTitle { get; set; }

    public bool DisableWatermarks { get; set; }
}
