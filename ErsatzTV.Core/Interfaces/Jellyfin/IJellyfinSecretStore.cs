using System.Threading.Tasks;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Jellyfin
{
    public interface IJellyfinSecretStore
    {
        Task<Unit> DeleteAll();
        Task<JellyfinSecrets> ReadSecrets();
        Task<Unit> SaveSecrets(JellyfinSecrets jellyfinSecrets);
    }
}
