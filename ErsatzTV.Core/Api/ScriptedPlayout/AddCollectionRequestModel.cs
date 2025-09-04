using System.ComponentModel;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

public record AddCollectionRequestModel
{
    [Description("Unique name used to reference this content throughout the scripted schedule")]
    public string Key { get; init; }

    [Description("The name of the existing manual collection")]
    public string Collection { get; init; }

    [Description("The playback order; only chronological and shuffle are currently supported")]
    public string Order { get; init; } = "shuffle";
}
