using ErsatzTV.Core.Emby;

namespace ErsatzTV.Core.Domain;

public class EmbyLibrary : Library
{
    public string ItemId { get; set; }
    public bool ShouldSyncItems { get; set; }
    public List<EmbyPathInfo> PathInfos { get; set; }
}
