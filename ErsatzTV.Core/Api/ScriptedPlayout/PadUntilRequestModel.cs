namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record PadUntilRequestModel
{
    public string Content { get; set; }
    public string When { get; set; }
    public bool Tomorrow { get; set; }
    public string Fallback { get; set; }
    public bool Trim { get; set; }
    public int DiscardAttempts { get; set; }
    public bool StopBeforeEnd { get; set; } = true;
    public bool OfflineTail { get; set; }
    public string FillerKind { get; set; }
    public string CustomTitle { get; set; }
    public bool DisableWatermarks { get; set; }
}
