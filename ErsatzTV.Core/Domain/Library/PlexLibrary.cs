namespace ErsatzTV.Core.Domain;

public class PlexLibrary : Library
{
    public string Key { get; set; }
    public bool ShouldSyncItems { get; set; }
}
