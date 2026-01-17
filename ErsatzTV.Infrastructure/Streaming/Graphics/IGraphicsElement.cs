using ErsatzTV.Core.Interfaces.Streaming;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public interface IGraphicsElement
{
    int ZIndex { get; }

    string DebugKey { get; }

    bool IsFinished { get; set; }

    Task InitializeAsync(GraphicsEngineContext context, CancellationToken cancellationToken);

    ValueTask<Option<PreparedElementImage>> PrepareImage(
        TimeSpan timeOfDay,
        TimeSpan contentTime,
        TimeSpan contentTotalTime,
        TimeSpan channelTime,
        CancellationToken cancellationToken);
}
