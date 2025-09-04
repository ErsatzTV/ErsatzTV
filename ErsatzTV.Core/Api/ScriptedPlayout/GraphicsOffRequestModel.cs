using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record GraphicsOffRequestModel
{
    [Description("A list of graphics elements to turn off. All graphics elements will be turned off if this list is null or empty")]
    public List<string> Graphics { get; set; } = [];
}
