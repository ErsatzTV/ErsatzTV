using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IConfigElementRepository
{
    Task<Unit> Upsert<T>(ConfigElementKey configElementKey, T value, CancellationToken cancellationToken);
    Task<Option<ConfigElement>> GetConfigElement(ConfigElementKey key, CancellationToken cancellationToken);
    Task<Option<T>> GetValue<T>(ConfigElementKey key, CancellationToken cancellationToken);
    Task Delete(ConfigElement configElement, CancellationToken cancellationToken);
    Task<Unit> Delete(ConfigElementKey configElementKey, CancellationToken cancellationToken);
}
