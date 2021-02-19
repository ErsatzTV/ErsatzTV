using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IConfigElementRepository
    {
        Task<ConfigElement> Add(ConfigElement configElement);
        Task<Option<ConfigElement>> Get(ConfigElementKey key);
        Task<Option<T>> GetValue<T>(ConfigElementKey key);
        Task Update(ConfigElement configElement);
        Task Delete(ConfigElement configElement);
    }
}
