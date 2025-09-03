namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddCollectionRequestModel
{
    public string Key { get; init; }
    public string Collection { get; init; }
    public string Order { get; init; } = "shuffle";
}
