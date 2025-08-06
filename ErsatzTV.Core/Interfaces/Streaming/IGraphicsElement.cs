using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Streaming;

public interface IGraphicsElement
{
    bool IsFailed { get; set; }

    Task InitializeAsync(Resolution frameSize, int frameRate, CancellationToken cancellationToken);

    void Draw(
        object context,
        TimeSpan timeOfDay,
        TimeSpan contentTime,
        TimeSpan channelTime,
        CancellationToken cancellationToken);
}