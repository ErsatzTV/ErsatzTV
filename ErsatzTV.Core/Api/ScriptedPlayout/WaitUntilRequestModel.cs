namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record WaitUntilRequestModel
{
    public string When { get; set; }
    public bool Tomorrow { get; set; }
    public bool RewindOnReset { get; set; }
}
