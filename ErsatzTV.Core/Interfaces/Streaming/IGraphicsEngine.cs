using System.IO.Pipelines;

namespace ErsatzTV.Core.Interfaces.Streaming;

public interface IGraphicsEngine
{
    Task Run(GraphicsEngineContext context, PipeWriter pipeWriter, CancellationToken cancellationToken);
}