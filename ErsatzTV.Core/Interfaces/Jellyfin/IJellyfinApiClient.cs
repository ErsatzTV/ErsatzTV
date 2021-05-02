using System.Threading.Tasks;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Jellyfin
{
    public interface IJellyfinApiClient
    {
        Task<Either<BaseError, string>> GetServerName(JellyfinSecrets secrets);
    }
}
