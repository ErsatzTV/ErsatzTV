namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IFFmpegSegmenterService
{
    event EventHandler OnWorkersChanged;
    ICollection<IHlsSessionWorker> Workers { get; }
    bool TryGetWorker(string channelNumber, out IHlsSessionWorker worker);
    bool TryAddWorker(string channelNumber, IHlsSessionWorker worker);
    void AddOrUpdateWorker(string channelNumber, IHlsSessionWorker worker);
    void RemoveWorker(string channelNumber, out IHlsSessionWorker inactiveWorker);
    bool IsActive(string channelNumber);
    Task<bool> StopChannel(string channelNumber, CancellationToken cancellationToken);
    void TouchChannel(string channelNumber);
    void PlayoutUpdated(string channelNumber);
}
