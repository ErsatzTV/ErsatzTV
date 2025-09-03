namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record StartEpgGroupRequestModel
{
    public bool Advance { get; set; } = true;
}
