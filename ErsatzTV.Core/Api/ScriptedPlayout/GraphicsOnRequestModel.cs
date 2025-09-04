using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record GraphicsOnRequestModel
{
    [Description("A list of graphics elements to turn on.")]
    public List<string> Graphics { get; set; }

    public Dictionary<string, string> Variables { get; set; } = [];
}
