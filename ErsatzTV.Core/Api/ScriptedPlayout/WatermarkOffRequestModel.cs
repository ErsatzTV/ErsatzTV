namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record WatermarkOffRequestModel
{
    public List<string> Watermark { get; set; } = [];
}
