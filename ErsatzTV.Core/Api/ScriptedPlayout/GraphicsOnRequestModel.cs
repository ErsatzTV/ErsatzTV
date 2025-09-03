namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record GraphicsOnRequestModel
{
    public List<string> Graphics { get; set; }
    public Dictionary<string, string> Variables { get; set; } = [];
}
