using ErsatzTV.Core.Scheduling.Engine;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IScriptedPlayoutBuilderService
{
    bool MockSession(ISchedulingEngine schedulingEngine, Guid buildId);
    Guid StartSession(ISchedulingEngine schedulingEngine);
    ISchedulingEngine GetEngine(Guid buildId);
    void EndSession(Guid buildId);
}
