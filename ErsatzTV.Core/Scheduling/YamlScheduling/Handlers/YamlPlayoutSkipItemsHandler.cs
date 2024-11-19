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

        if (skipItems.SkipItems < 0)
        {
            logger.LogWarning("Unable to skip invalid number: {Skip}", skipItems.SkipItems);
            return false;
        }

        Option<IMediaCollectionEnumerator> maybeEnumerator = await enumeratorCache.GetCachedEnumeratorForContent(
            context,
            skipItems.Content,
            cancellationToken);

        foreach (IMediaCollectionEnumerator enumerator in maybeEnumerator)
        {
            for (var i = 0; i < skipItems.SkipItems; i++)
            {
                enumerator.MoveNext();
            }
        }

        return true;
    }
}
