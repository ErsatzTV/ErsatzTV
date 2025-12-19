using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;
using NCalc;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutSkipItemsHandler(EnumeratorCache enumeratorCache) : IYamlPlayoutHandler
{
    public bool Reset => true;

    public async Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        Func<string, Task> executeSequence,
        ILogger<SequentialPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutSkipItemsInstruction skipItems)
        {
            return false;
        }

        Option<IMediaCollectionEnumerator> maybeEnumerator = await enumeratorCache.GetCachedEnumeratorForContent(
            context,
            skipItems.Content,
            cancellationToken);

        foreach (IMediaCollectionEnumerator enumerator in maybeEnumerator)
        {
            int seed = context.Playout.Seed + context.InstructionIndex + context.CurrentTime.DayOfYear;
            var random = new Random(seed);
            int enumeratorCount = enumerator is PlaylistEnumerator playlistEnumerator
                ? playlistEnumerator.CountForRandom
                : enumerator.Count;
            var expression = new Expression(skipItems.SkipItems);
            expression.EvaluateParameter += (name, e) =>
            {
                e.Result = name switch
                {
                    "count" => enumeratorCount,
                    "random" => random.Next() % enumeratorCount,
                    _ => e.Result
                };
            };

            object expressionResult = expression.Evaluate(cancellationToken);
            int skipCount = expressionResult switch
            {
                double doubleResult => (int)Math.Floor(doubleResult),
                int intResult => intResult,
                _ => 0
            };

            skipCount %= enumerator.Count;

            if (skipCount < 0)
            {
                logger.LogWarning("Unable to skip invalid number: {Skip}", skipItems.SkipItems);
                return false;
            }

            for (var i = 0; i < skipCount; i++)
            {
                enumerator.MoveNext(Option<DateTimeOffset>.None);
            }
        }

        return true;
    }
}
