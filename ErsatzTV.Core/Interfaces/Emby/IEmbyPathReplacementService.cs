using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Emby;

public interface IEmbyPathReplacementService
{
    Task<string> GetReplacementEmbyPath(int libraryPathId, string path, bool log = true);
    string GetReplacementEmbyPath(List<EmbyPathReplacement> pathReplacements, string path, bool log = true);
}
