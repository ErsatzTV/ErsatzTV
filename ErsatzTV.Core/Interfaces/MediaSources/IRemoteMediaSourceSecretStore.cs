using System.Threading.Tasks;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.MediaSources
{
    public interface IRemoteMediaSourceSecretStore<TSecrets>
    {
        Task<Unit> DeleteAll();
        Task<TSecrets> ReadSecrets();
        Task<Unit> SaveSecrets(TSecrets jellyfinSecrets);
    }
}
