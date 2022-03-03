using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class ConfigElementRepository : IConfigElementRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public ConfigElementRepository(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Unit> Upsert<T>(ConfigElementKey configElementKey, T value)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();

        Option<ConfigElement> maybeElement = await dbContext.ConfigElements
            .SelectOneAsync(c => c.Key, c => c.Key == configElementKey.Key);

        await maybeElement.Match(
            async element =>
            {
                element.Value = value.ToString();
                await dbContext.SaveChangesAsync();
            },
            async () =>
            {
                var configElement = new ConfigElement
                {
                    Key = configElementKey.Key,
                    Value = value.ToString()
                };

                await dbContext.ConfigElements.AddAsync(configElement);
                await dbContext.SaveChangesAsync();
            });

        return Unit.Default;
    }

    public async Task<Option<ConfigElement>> Get(ConfigElementKey key)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.ConfigElements
            .OrderBy(ce => ce.Key)
            .SingleOrDefaultAsync(ce => ce.Key == key.Key)
            .Map(Optional);
    }

    public Task<Option<T>> GetValue<T>(ConfigElementKey key) =>
        Get(key).MapT(ce => (T) Convert.ChangeType(ce.Value, typeof(T)));

    public async Task Delete(ConfigElement configElement)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        dbContext.ConfigElements.Remove(configElement);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Unit> Delete(ConfigElementKey configElementKey)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        Option<ConfigElement> maybeExisting = await dbContext.ConfigElements
            .SelectOneAsync(ce => ce.Key, ce => ce.Key == configElementKey.Key);
        foreach (ConfigElement element in maybeExisting)
        {
            dbContext.ConfigElements.Remove(element);
        }

        await dbContext.SaveChangesAsync();

        return Unit.Default;
    }
}