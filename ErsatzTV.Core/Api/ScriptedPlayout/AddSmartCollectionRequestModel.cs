using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddSmartCollectionRequestModel
{
    [Description("Unique name used to reference this content throughout the scripted schedule")]
    public string Key { get; set; }

    [Description("The name of the existing smart collection")]
    public string SmartCollection { get; set; }

    [Description("The playback order; only chronological and shuffle are currently supported")]
    public string Order { get; set; } = "shuffle";
}
