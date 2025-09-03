namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddMarathonRequestModel
{
    public string Key { get; set; }
    public string GroupBy { get; set; }
    public string ItemOrder { get; set; } = "shuffle";
    public Dictionary<string, List<string>> Guids { get; set; } = [];
    public List<string> Searches { get; set; } = [];
    public bool PlayAllItems { get; set; }
    public bool ShuffleGroups { get; set; }
}
