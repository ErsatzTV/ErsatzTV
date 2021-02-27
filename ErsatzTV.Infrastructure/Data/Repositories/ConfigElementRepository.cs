using System;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class ConfigElementRepository : IConfigElementRepository
    {
        private readonly TvContext _dbContext;

        public ConfigElementRepository(TvContext dbContext) => _dbContext = dbContext;

        public async Task<ConfigElement> Add(ConfigElement configElement)
        {
            await _dbContext.ConfigElements.AddAsync(configElement);
            await _dbContext.SaveChangesAsync();
            return configElement;
        }

        public Task<Option<ConfigElement>> Get(ConfigElementKey key) =>
            _dbContext.ConfigElements
                .OrderBy(ce => ce.Key)
                .SingleOrDefaultAsync(ce => ce.Key == key.Key)
                .Map(Optional);

        public Task<Option<T>> GetValue<T>(ConfigElementKey key) =>
            Get(key).MapT(ce => (T) Convert.ChangeType(ce.Value, typeof(T)));

        public async Task Update(ConfigElement configElement)
        {
            _dbContext.ConfigElements.Update(configElement);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(ConfigElement configElement)
        {
            _dbContext.ConfigElements.Remove(configElement);
            await _dbContext.SaveChangesAsync();
        }
    }
}
