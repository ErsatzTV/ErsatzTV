using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Streaming;

public interface IGraphicsElement
{
    Task InitializeAsync(Resolution frameSize, int frameRate, CancellationToken cancellationToken);

    void Draw(object context, TimeSpan timestamp);
}