using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public class UniqueIdNfo
{
    public bool Default { get; set; }
    public string? Type { get; set; }

    [SuppressMessage("Naming", "CA1720:Identifier contains type name")]
    public string? Guid { get; set; }
}
