using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Streaming;

public interface IGraphicsElement
{
    int ZIndex { get; }

    bool IsFailed { get; set; }

    Task InitializeAsync(Resolution frameSize, int frameRate, CancellationToken cancellationToken);

    void Draw(
        object context,
        TimeSpan timeOfDay,
        TimeSpan contentTime,
        TimeSpan contentTotalTime,
        TimeSpan channelTime,
        CancellationToken cancellationToken);
}