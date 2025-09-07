using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.Engine;

namespace ErsatzTV.Core.Scheduling.ScriptedScheduling;

public class ScriptedPlayoutBuilderService : IScriptedPlayoutBuilderService
{
    private readonly ConcurrentDictionary<Guid, ISchedulingEngine> _sessions = new();

    public bool MockSession(ISchedulingEngine schedulingEngine, Guid buildId) =>
        _sessions.TryAdd(buildId, schedulingEngine);

    public Guid StartSession(ISchedulingEngine schedulingEngine)
    {
        var buildId = Guid.NewGuid();
        _sessions[buildId] = schedulingEngine;
        return buildId;
    }

    public ISchedulingEngine GetEngine(Guid buildId) => _sessions.GetValueOrDefault(buildId);

    public void EndSession(Guid buildId)
    {
        _sessions.TryRemove(buildId, out _);
    }
}
