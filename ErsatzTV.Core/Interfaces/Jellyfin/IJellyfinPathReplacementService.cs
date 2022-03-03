using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Jellyfin;

public interface IJellyfinPathReplacementService
{
    Task<string> GetReplacementJellyfinPath(int libraryPathId, string path);
    string GetReplacementJellyfinPath(List<JellyfinPathReplacement> pathReplacements, string path, bool log = true);
}