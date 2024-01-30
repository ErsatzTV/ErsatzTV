using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.FFmpeg;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegSegmenterService(ILogger<FFmpegSegmenterService> logger) : IFFmpegSegmenterService
{
    private readonly ConcurrentDictionary<string, IHlsSessionWorker> _sessionWorkers = new();

    public event EventHandler OnWorkersChanged;

    public ICollection<IHlsSessionWorker> Workers => _sessionWorkers.Values;
    
    public bool TryGetWorker(string channelNumber, out IHlsSessionWorker worker) =>
        _sessionWorkers.TryGetValue(channelNumber, out worker);

    public bool TryAddWorker(string channelNumber, IHlsSessionWorker worker)
    {
        bool result = _sessionWorkers.TryAdd(channelNumber, worker);

        if (result)
        {
            OnWorkersChanged?.Invoke(this, EventArgs.Empty);
        }
        
        return result;
    }

    public void AddOrUpdateWorker(string channelNumber, IHlsSessionWorker worker)
    {
        _sessionWorkers.AddOrUpdate(channelNumber, _ => worker, (_, _) => worker);
        OnWorkersChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveWorker(string channelNumber, out IHlsSessionWorker inactiveWorker)
    {
        _sessionWorkers.TryRemove(channelNumber, out inactiveWorker);
        OnWorkersChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsActive(string channelNumber) => _sessionWorkers.ContainsKey(channelNumber);

    public async Task<bool> StopChannel(string channelNumber, CancellationToken cancellationToken)
    {
        if (_sessionWorkers.TryGetValue(channelNumber, out IHlsSessionWorker worker))
        {
            if (worker != null)
            {
                await worker.Cancel(cancellationToken);
                return true;
            }
        }

        return false;
    }

    public void TouchChannel(string channelNumber)
    {
        if (_sessionWorkers.TryGetValue(channelNumber, out IHlsSessionWorker worker))
        {
            worker?.Touch();
        }
    }

    public void PlayoutUpdated(string channelNumber)
    {
        if (_sessionWorkers.TryGetValue(channelNumber, out IHlsSessionWorker worker))
        {
            if (worker != null)
            {
                logger.LogInformation(
                    "Playout has been updated for channel {ChannelNumber}, HLS segmenter will skip ahead to catch up",
                    channelNumber);

                worker.PlayoutUpdated();
            }
        }
    }
}
