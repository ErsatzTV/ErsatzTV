using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record ControlWatermarkOff
{
    [Description("A list of existing watermark names to turn off. All (scripted) watermarks will be turned off if this list is null or empty.")]
    public List<string> Watermark { get; set; } = [];
}
