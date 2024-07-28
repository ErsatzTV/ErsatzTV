using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutShuffleSequenceHandler : IYamlPlayoutHandler
{
    public bool Reset => false;

    public Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutShuffleSequenceInstruction shuffleSequenceInstruction)
        {
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(shuffleSequenceInstruction.ShuffleSequence))
        {
            logger.LogWarning("Sequence key is required to shuffle sequence");
            return Task.FromResult(false);
        }

        string sequenceKey = shuffleSequenceInstruction.ShuffleSequence;

        var groupedSequenceItems = context.Definition.Playout
            .Where(i => i.SequenceKey == sequenceKey)
            .GroupBy(i => i.SequenceGuid)
            .ToList();

        foreach (IGrouping<Guid, YamlPlayoutInstruction> grouping in groupedSequenceItems)
        {
            // shuffle, avoiding starting with the tail of the last shuffle
            YamlPlayoutInstruction tail = grouping.Last();
            var shuffledGroup = grouping.OrderBy(_ => Guid.NewGuid()).ToList();
            while (shuffledGroup.Count > 1 && shuffledGroup.Head() == tail)
            {
                shuffledGroup = grouping.OrderBy(_ => Guid.NewGuid()).ToList();
            }

            int firstIndex = context.Definition.Playout.FindIndex(i => i.SequenceGuid == grouping.Key);
            context.Definition.Playout.RemoveRange(firstIndex, shuffledGroup.Count);
            context.Definition.Playout.InsertRange(firstIndex, shuffledGroup);
        }

        return Task.FromResult(true);
    }
}
