namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddShowRequestModel
{
    public string Key { get; set; }
    public Dictionary<string, string> Guids { get; set; } = [];
    public string Order { get; set; } = "shuffle";
}
