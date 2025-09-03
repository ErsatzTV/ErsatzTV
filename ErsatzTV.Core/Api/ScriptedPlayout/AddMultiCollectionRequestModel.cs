namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddMultiCollectionRequestModel
{
    public string Key { get; set; }
    public string MultiCollection { get; set; }
    public string Order { get; set; } = "shuffle";
}
