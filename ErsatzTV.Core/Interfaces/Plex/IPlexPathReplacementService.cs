using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Plex;

public interface IPlexPathReplacementService
{
    Task<string> GetReplacementPlexPath(int libraryPathId, string path, bool log = true);
    string GetReplacementPlexPath(List<PlexPathReplacement> pathReplacements, string path, bool log = true);
}
