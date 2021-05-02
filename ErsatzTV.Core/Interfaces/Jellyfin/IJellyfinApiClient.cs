using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Jellyfin
{
    public interface IJellyfinApiClient
    {
        Task<Either<BaseError, string>> GetServerName(JellyfinSecrets secrets);
        Task<Either<BaseError, List<JellyfinLibrary>>> GetLibraries(string address, string apiKey);
    }
}
