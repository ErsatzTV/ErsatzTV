namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddAllRequestModel
{
    public string Content { get; set; }
    public string FillerKind { get; set; }
    public string CustomTitle { get; set; }
    public bool DisableWatermarks { get; set; }
}
