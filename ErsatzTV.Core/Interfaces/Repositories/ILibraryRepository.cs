using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ILibraryRepository
    {
        Task<Option<Library>> Get(int libraryId);
        Task<List<LocalLibrary>> GetAllLocal();
        Task<Unit> UpdateLastScan(Library library);
        Task<List<LibraryPath>> GetLocalPaths(int libraryId);
    }
}
