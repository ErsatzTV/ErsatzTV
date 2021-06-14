using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IConfigElementRepository
    {
        Task<Unit> Upsert<T>(ConfigElementKey configElementKey, T value);
        Task<Option<ConfigElement>> Get(ConfigElementKey key);
        Task<Option<T>> GetValue<T>(ConfigElementKey key);
        Task Delete(ConfigElement configElement);
    }
}
