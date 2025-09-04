namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddSearchQueryRequestModel
{
    public string Key { get; set; }
    public string Query { get; set; }
    public string Order { get; set; } = "shuffle";
}
