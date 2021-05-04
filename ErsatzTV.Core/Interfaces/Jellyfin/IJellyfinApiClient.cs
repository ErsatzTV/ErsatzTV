using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Jellyfin
{
    public interface IJellyfinApiClient
    {
        Task<Either<BaseError, string>> GetServerName(string address, string apiKey);
        Task<Either<BaseError, List<JellyfinLibrary>>> GetLibraries(string address, string apiKey);
        Task<Either<BaseError, string>> GetAdminUserId(string address, string apiKey);

        Task<Either<BaseError, List<string>>> GetLibraryItems(
            string address,
            string apiKey,
            int mediaSourceId,
            string libraryId);
    }
}
