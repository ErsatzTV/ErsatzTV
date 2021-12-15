using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaItemRepository
    {
        Task<List<string>> GetAllLanguageCodes();
        Task<List<int>> FlagFileNotFound(LibraryPath libraryPath, string path);
        Task<Unit> FlagNormal(MediaItem mediaItem);
    }
}
