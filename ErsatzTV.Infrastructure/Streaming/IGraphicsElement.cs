using ErsatzTV.Core.Domain;

namespace ErsatzTV.Infrastructure.Streaming;

public interface IGraphicsElement
{
    int ZIndex { get; }

    bool IsFailed { get; set; }

    Task InitializeAsync(
        Resolution squarePixelFrameSize,
        Resolution frameSize,
        int frameRate,
        CancellationToken cancellationToken);

    ValueTask<Option<PreparedElementImage>> PrepareImage(
        TimeSpan timeOfDay,
        TimeSpan contentTime,
        TimeSpan contentTotalTime,
        TimeSpan channelTime,
        CancellationToken cancellationToken);
}