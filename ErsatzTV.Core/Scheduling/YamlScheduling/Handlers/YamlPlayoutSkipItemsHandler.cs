using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutSkipItemsHandler(EnumeratorCache enumeratorCache) : IYamlPlayoutHandler
{
    public bool Reset => true;

    public async Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        ILogger<YamlPlayoutBuilder> logger,
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
            var expression = new NCalc.Expression(skipItems.SkipItems);
            expression.EvaluateParameter += (name, e) =>
            {
                e.Result = name switch
                {
                    "count" => enumerator.Count,
                    "random" => new Random().Next() % enumerator.Count,
                    _ => e.Result
                };
            };

            object expressionResult = expression.Evaluate();
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
                enumerator.MoveNext();
            }
        }

        return true;
    }
}
