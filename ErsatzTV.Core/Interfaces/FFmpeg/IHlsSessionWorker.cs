using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IHlsSessionWorker : IDisposable
{
    Task Cancel(CancellationToken cancellationToken);
    void Touch(Option<string> fileName);
    Task<Option<TrimPlaylistResult>> TrimPlaylist(DateTimeOffset filterBefore, CancellationToken cancellationToken);
    void PlayoutUpdated();
    HlsSessionModel GetModel();
    Task Run(string channelNumber, Option<TimeSpan> idleTimeout, CancellationToken incomingCancellationToken);
    Task WaitForPlaylistSegments(int initialSegmentCount, CancellationToken cancellationToken);
}
