using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Streaming;

public interface IGraphicsElementLoader
{
    Task<GraphicsEngineContext> LoadAll(
        GraphicsEngineContext context,
        List<PlayoutItemGraphicsElement> elements,
        CancellationToken cancellationToken);

    Task<Option<string>> TryLoadName(string fileName, CancellationToken cancellationToken);
}
