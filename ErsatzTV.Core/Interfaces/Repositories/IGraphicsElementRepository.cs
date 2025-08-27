using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IGraphicsElementRepository
{
    Task<Option<GraphicsElement>> GetGraphicsElementByPath(string path, CancellationToken cancellationToken);
}
