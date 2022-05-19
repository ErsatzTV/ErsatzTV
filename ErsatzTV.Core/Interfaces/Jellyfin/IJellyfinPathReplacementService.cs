using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Jellyfin;

public interface IJellyfinPathReplacementService
{
    Task<string> GetReplacementJellyfinPath(int libraryPathId, string path, bool log = true);
    string GetReplacementJellyfinPath(List<JellyfinPathReplacement> pathReplacements, string path, bool log = true);
    string ReplaceNetworkPath(JellyfinMediaSource mediaSource, string path, string networkPath, string replacement);
}
