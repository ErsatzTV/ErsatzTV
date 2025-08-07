using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.Troubleshooting;

namespace ErsatzTV.Core.Troubleshooting;

public class TroubleshootingNotifier : ITroubleshootingNotifier
{
    private readonly ConcurrentDictionary<Guid, bool> _failedSessions = new();

    public bool IsFailed(Guid sessionId)
    {
        return _failedSessions.TryGetValue(sessionId, out _);
    }

    public void NotifyFailed(Guid sessionId)
    {
        _failedSessions[sessionId] = true;
    }

    public void RemoveSession(Guid sessionId)
    {
        _failedSessions.TryRemove(sessionId, out _);
    }
}