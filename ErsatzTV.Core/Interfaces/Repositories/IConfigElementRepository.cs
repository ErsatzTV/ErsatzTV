using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IConfigElementRepository
    {
        public Task<ConfigElement> Add(ConfigElement configElement);
        public Task<Option<ConfigElement>> Get(ConfigElementKey key);
        public Task<Option<T>> GetValue<T>(ConfigElementKey key);
        public Task Update(ConfigElement configElement);
        public Task Delete(ConfigElement configElement);
    }
}
