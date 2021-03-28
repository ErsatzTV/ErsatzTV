using System.Threading.Tasks;

namespace ErsatzTV.Core.Interfaces.Plex
{
    public interface IPlexPathReplacementService
    {
        Task<string> GetReplacementPlexPath(int libraryPathId, string path);
    }
}
