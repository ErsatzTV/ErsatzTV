using ErsatzTV.Core.Domain;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public interface IGraphicsElement
{
    int ZIndex { get; }

    bool IsFinished { get; set; }

    Task InitializeAsync(
        Resolution squarePixelFrameSize,
        Resolution frameSize,
        int frameRate,
        TimeSpan seek,
        CancellationToken cancellationToken);

    ValueTask<Option<PreparedElementImage>> PrepareImage(
        TimeSpan timeOfDay,
        TimeSpan contentTime,
        TimeSpan contentTotalTime,
        TimeSpan channelTime,
        CancellationToken cancellationToken);
}
