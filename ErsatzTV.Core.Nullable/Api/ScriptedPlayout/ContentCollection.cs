using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public record ContentCollection
{
    [Description("Unique name used to reference this content throughout the scripted schedule")]
    public required string Key { get; init; }

    [Description("The name of the existing manual collection")]
    public required string Collection { get; init; }

    [Description("The playback order; only chronological and shuffle are currently supported")]
    public string Order { get; init; } = "shuffle";
}
