using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.FFmpeg;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class GraphicsElementSelector(IDecoSelector decoSelector, ILogger<GraphicsElementSelector> logger)
    : IGraphicsElementSelector
{
    public List<PlayoutItemGraphicsElement> SelectGraphicsElements(
        Channel channel,
        PlayoutItem playoutItem,
        DateTimeOffset now)
    {
        logger.LogDebug("Checking for graphics elements at {Now}", now);

        var result = new List<PlayoutItemGraphicsElement>();

        if (channel.StreamingMode == StreamingMode.HttpLiveStreamingDirect)
        {
            return result;
        }

        // if (playoutItem.DisableWatermarks)
        // {
        //     logger.LogDebug("Graphics elements are disabled by playout item");
        //     return result;
        // }

        DecoEntries decoEntries = decoSelector.GetDecoEntries(playoutItem.Playout, now);

        // first, check deco template / active deco
        foreach (Deco templateDeco in decoEntries.TemplateDeco)
        {
            var done = false;

            switch (templateDeco.GraphicsElementsMode)
            {
                case DecoMode.Merge:
                    if (playoutItem.FillerKind is FillerKind.None || templateDeco.UseGraphicsElementsDuringFiller)
                    {
                        logger.LogDebug("Graphics elements will come from template deco (merge)");
                        result.AddRange(
                            templateDeco.DecoGraphicsElements.Map(dge => dge.GraphicsElement).Map(ge =>
                                new PlayoutItemGraphicsElement { PlayoutItem = playoutItem, GraphicsElement = ge }));
                        break;
                    }

                    logger.LogDebug("Graphics elements are disabled by template deco during filler");
                    result.Clear();
                    done = true;
                    break;
                case DecoMode.Override:
                    if (playoutItem.FillerKind is FillerKind.None || templateDeco.UseGraphicsElementsDuringFiller)
                    {
                        logger.LogDebug("Graphics elements will come from template deco (replace)");
                        result.AddRange(
                            templateDeco.DecoGraphicsElements.Map(dge => dge.GraphicsElement).Map(ge =>
                                new PlayoutItemGraphicsElement { PlayoutItem = playoutItem, GraphicsElement = ge }));
                        done = true;
                        break;
                    }

                    logger.LogDebug("Graphics elements are disabled by template deco during filler");
                    result.Clear();
                    done = true;
                    break;
                case DecoMode.Disable:
                    logger.LogDebug("Graphics elements are disabled by template deco");
                    done = true;
                    break;
                case DecoMode.Inherit:
                    logger.LogDebug("Graphics elements will inherit from playout deco");
                    break;
            }

            if (done)
            {
                return result;
            }
        }

        // second, check playout deco
        foreach (Deco playoutDeco in decoEntries.PlayoutDeco)
        {
            var done = false;

            switch (playoutDeco.GraphicsElementsMode)
            {
                case DecoMode.Merge:
                    if (playoutItem.FillerKind is FillerKind.None || playoutDeco.UseGraphicsElementsDuringFiller)
                    {
                        logger.LogDebug("Graphics elements will come from playout deco (merge)");
                        result.AddRange(
                            playoutDeco.DecoGraphicsElements.Map(dge => dge.GraphicsElement).Map(ge =>
                                new PlayoutItemGraphicsElement { PlayoutItem = playoutItem, GraphicsElement = ge }));
                        break;
                    }

                    logger.LogDebug("Graphics elements are disabled by playout deco during filler");
                    result.Clear();
                    done = true;
                    break;
                case DecoMode.Override:
                    if (playoutItem.FillerKind is FillerKind.None || playoutDeco.UseGraphicsElementsDuringFiller)
                    {
                        logger.LogDebug("Graphics elements will come from playout deco (replace)");
                        result.AddRange(
                            playoutDeco.DecoGraphicsElements.Map(dge => dge.GraphicsElement).Map(ge =>
                                new PlayoutItemGraphicsElement { PlayoutItem = playoutItem, GraphicsElement = ge }));
                        done = true;
                        break;
                    }

                    logger.LogDebug("Graphics elements are disabled by playout deco during filler");
                    result.Clear();
                    done = true;
                    break;
                case DecoMode.Disable:
                    logger.LogDebug("Graphics elements are disabled by playout deco");
                    done = true;
                    break;
                case DecoMode.Inherit:
                    logger.LogDebug("Graphics elements will inherit from channel and/or global setting");
                    break;
            }

            if (done)
            {
                return result;
            }
        }

        result.AddRange(playoutItem.PlayoutItemGraphicsElements);

        return result;
    }
}
