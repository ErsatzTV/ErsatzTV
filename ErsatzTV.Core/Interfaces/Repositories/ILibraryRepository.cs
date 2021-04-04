using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ILibraryRepository
    {
        Task<LibraryPath> Add(LibraryPath libraryPath);
        Task<Option<Library>> Get(int libraryId);
        Task<Option<LocalLibrary>> GetLocal(int libraryId);
        Task<List<Library>> GetAll();
        Task<Unit> UpdateLastScan(Library library);
        Task<Unit> UpdateLastScan(LibraryPath libraryPath);
        Task<List<LibraryPath>> GetLocalPaths(int libraryId);
        Task<Option<LibraryPath>> GetPath(int libraryPathId);
        Task<int> CountMediaItemsByPath(int libraryPathId);
        Task<List<int>> GetMediaIdsByLocalPath(int libraryPathId);
        Task DeleteLocalPath(int libraryPathId);
    }
}
