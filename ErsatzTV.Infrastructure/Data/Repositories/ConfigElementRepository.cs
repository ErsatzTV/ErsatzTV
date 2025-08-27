using System.Globalization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class ConfigElementRepository(IDbContextFactory<TvContext> dbContextFactory) : IConfigElementRepository
{
    public async Task<Unit> Upsert<T>(ConfigElementKey configElementKey, T value, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<ConfigElement> maybeElement = await dbContext.ConfigElements
            .SelectOneAsync(c => c.Key, c => c.Key == configElementKey.Key, cancellationToken);

        await maybeElement.Match(
            async element =>
            {
                element.Value = value.ToString();
                await dbContext.SaveChangesAsync(cancellationToken);
            },
            async () =>
            {
                var configElement = new ConfigElement
                {
                    Key = configElementKey.Key,
                    Value = value.ToString()
                };

                await dbContext.ConfigElements.AddAsync(configElement, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            });

        return Unit.Default;
    }

    public async Task<Option<ConfigElement>> GetConfigElement(ConfigElementKey key, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ConfigElements
            .AsNoTracking()
            .SelectOneAsync(ce => ce.Key, ce => ce.Key == key.Key, cancellationToken);
    }

    public Task<Option<T>> GetValue<T>(ConfigElementKey key, CancellationToken cancellationToken) =>
        GetConfigElement(key, cancellationToken).MapT(ce =>
        {
            if (typeof(T).Name == "Guid")
            {
                return (T)Convert.ChangeType(Guid.Parse(ce.Value), typeof(T), CultureInfo.InvariantCulture);
            }

            if (typeof(T).IsEnum)
            {
                return (T)Enum.Parse(typeof(T), ce.Value);
            }

            return (T)Convert.ChangeType(ce.Value, typeof(T), CultureInfo.InvariantCulture);
        });

    public async Task Delete(ConfigElement configElement, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ConfigElements.Remove(configElement);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Unit> Delete(ConfigElementKey configElementKey, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Option<ConfigElement> maybeExisting = await dbContext.ConfigElements
            .SelectOneAsync(ce => ce.Key, ce => ce.Key == configElementKey.Key, cancellationToken);
        foreach (ConfigElement element in maybeExisting)
        {
            dbContext.ConfigElements.Remove(element);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Default;
    }
}
