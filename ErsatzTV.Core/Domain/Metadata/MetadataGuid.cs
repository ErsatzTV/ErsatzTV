using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.Domain;

public class MetadataGuid
{
    public int Id { get; set; }

    [SuppressMessage("Naming", "CA1720:Identifier contains type name")]
    public string Guid { get; set; }
}
