using System.Threading.Tasks;

namespace ErsatzTV.Core.Interfaces.Jellyfin
{
    public interface IJellyfinPathReplacementService
    {
        Task<string> GetReplacementJellyfinPath(int libraryPathId, string path);
    }
}
