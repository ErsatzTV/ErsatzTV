using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling.TemplateScheduling;

public class TemplatePlayoutBuilder : ITemplatePlayoutBuilder
{
    public Task<Playout> Build(Playout playout, PlayoutBuildMode mode, CancellationToken cancellationToken)
    {
        return Task.FromResult(playout);
    }
}
