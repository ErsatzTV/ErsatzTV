namespace ErsatzTV.Core.Domain;

public class JellyfinLibrary : Library
{
    public string ItemId { get; set; }
    public bool ShouldSyncItems { get; set; }
}