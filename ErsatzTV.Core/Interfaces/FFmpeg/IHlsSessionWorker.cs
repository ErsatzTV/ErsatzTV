using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IHlsSessionWorker : IDisposable
{
    Task Cancel(CancellationToken cancellationToken);
    void Touch();
    Task<Option<TrimPlaylistResult>> TrimPlaylist(DateTimeOffset filterBefore, CancellationToken cancellationToken);
    void PlayoutUpdated();
    HlsSessionModel GetModel();
}
