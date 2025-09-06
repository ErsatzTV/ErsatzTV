using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.Api.ScriptedPlayout;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public record ContentMultiCollection
{
    [Description("Unique name used to reference this content throughout the scripted schedule")]
    public string Key { get; set; }

    [Description("The name of the existing multi-collection")]
    public string MultiCollection { get; set; }

    [Description("The playback order; only chronological and shuffle are currently supported")]
    public string Order { get; set; } = "shuffle";
}
