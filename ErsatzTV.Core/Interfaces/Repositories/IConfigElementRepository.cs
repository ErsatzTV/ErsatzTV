using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IConfigElementRepository
{
    Task<Unit> Upsert<T>(ConfigElementKey configElementKey, T value);
    Task<Option<ConfigElement>> GetConfigElement(ConfigElementKey key);
    Task<Option<T>> GetValue<T>(ConfigElementKey key);
    Task Delete(ConfigElement configElement);
    Task<Unit> Delete(ConfigElementKey configElementKey);
}
