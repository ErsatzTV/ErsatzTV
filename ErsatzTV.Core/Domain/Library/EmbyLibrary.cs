namespace ErsatzTV.Core.Domain;

public class EmbyLibrary : Library
{
    public string ItemId { get; set; }
    public bool ShouldSyncItems { get; set; }
}
