using ErsatzTV.Core.Domain;

namespace ErsatzTV.Infrastructure.Extensions;

public static class ConfigElementQueryableExtensions
{
    public static Task<Option<T>> GetValue<T>(
        this IQueryable<ConfigElement> configElements,
        ConfigElementKey key) =>
        configElements
            .SelectOneAsync(ce => ce.Key, ce => ce.Key == key.Key)
            .MapT(ce => (T) Convert.ChangeType(ce.Value, typeof(T)));
}