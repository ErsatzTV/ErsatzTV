using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Emby;

public interface IEmbyPathReplacementService
{
    Task<string> GetReplacementEmbyPath(int libraryPathId, string path);
    string GetReplacementEmbyPath(List<EmbyPathReplacement> pathReplacements, string path, bool log = true);
}