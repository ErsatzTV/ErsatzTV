using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling.BlockScheduling;

internal record EffectiveBlock(Block Block, BlockKey BlockKey, DateTimeOffset Start, int TemplateItemId)
{
    public static List<EffectiveBlock> GetEffectiveBlocks(
        ICollection<PlayoutTemplate> templates,
        DateTimeOffset start,
        TimeZoneInfo timeZone,
        int daysToBuild)
    {
        DateTimeOffset finish = start.AddDays(daysToBuild);

        var effectiveBlocks = new List<EffectiveBlock>();
        DateTimeOffset current = new DateTimeOffset(start.Year, start.Month, start.Day, 0, 0, 0, start.Offset);
        while (current < finish)
        {
            Option<PlayoutTemplate> maybeTemplate = PlayoutTemplateSelector.GetPlayoutTemplateFor(templates, current);
            foreach (PlayoutTemplate playoutTemplate in maybeTemplate)
            {
                // logger.LogDebug(
                //     "Will schedule day {Date} using template {Template}",
                //     current,
                //     playoutTemplate.Template.Id);

                DateTimeOffset today = current;

                var newBlocks = playoutTemplate.Template.Items
                    .Map(i => ToEffectiveBlock(playoutTemplate, i, today, timeZone))
                    .Map(NormalizeGuideMode)
                    .ToList();

                effectiveBlocks.AddRange(newBlocks);
            }

            var localDate = TimeZoneInfo.ConvertTime(current, timeZone).Date;
            current = new DateTimeOffset(localDate.AddDays(1), timeZone.GetUtcOffset(localDate.AddDays(1)));
        }

        effectiveBlocks.RemoveAll(b => b.Start.AddMinutes(b.Block.Minutes) < start || b.Start > finish);
        effectiveBlocks = effectiveBlocks.OrderBy(rb => rb.Start).ToList();

        return effectiveBlocks;
    }

    private static EffectiveBlock ToEffectiveBlock(
        PlayoutTemplate playoutTemplate,
        TemplateItem templateItem,
        DateTimeOffset current,
        TimeZoneInfo timeZone)
    {
        var blockStartTime = new DateTime(
            current.Year,
            current.Month,
            current.Day,
            templateItem.StartTime.Hours,
            templateItem.StartTime.Minutes,
            0,
            DateTimeKind.Unspecified);

        var blockStart = new DateTimeOffset(blockStartTime, timeZone.GetUtcOffset(blockStartTime));

        // logger.LogDebug(
        //     "Starting block {Block} at {BlockStart}",
        //     templateItem.Block.Name,
        //     blockStart);

        return new EffectiveBlock(
            templateItem.Block,
            new BlockKey(templateItem.Block, templateItem.Template, playoutTemplate),
            blockStart,
            templateItem.Id);
    }

    private static EffectiveBlock NormalizeGuideMode(EffectiveBlock effectiveBlock)
    {
        if (effectiveBlock.Block.Items is not null &&
            effectiveBlock.Block.Items.All(bi => !bi.IncludeInProgramGuide))
        {
            foreach (BlockItem blockItem in effectiveBlock.Block.Items)
            {
                blockItem.IncludeInProgramGuide = true;
            }
        }

        return effectiveBlock;
    }
}
