using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record WatermarkOnRequestModel
{
    [Description("A list of existing watermark names to turn on")]
    public List<string> Watermark { get; set; }
}
