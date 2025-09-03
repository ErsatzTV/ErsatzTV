namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddSmartCollectionRequestModel
{
    public string Key { get; set; }
    public string SmartCollection { get; set; }
    public string Order { get; set; } = "shuffle";
}
