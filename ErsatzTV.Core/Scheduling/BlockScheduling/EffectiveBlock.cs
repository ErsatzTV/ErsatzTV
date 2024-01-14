using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling.BlockScheduling;

internal record EffectiveBlock(Block Block, BlockKey BlockKey, DateTimeOffset Start)
{
    public static List<EffectiveBlock> GetEffectiveBlocks(Playout playout, DateTimeOffset start, int daysToBuild)
    {
        DateTimeOffset finish = start.AddDays(daysToBuild);

        var effectiveBlocks = new List<EffectiveBlock>();
        DateTimeOffset current = start.Date;
        while (current < finish)
        {
            foreach (PlayoutTemplate playoutTemplate in PlayoutTemplateSelector.GetPlayoutTemplateFor(
                         playout.Templates,
                         current))
            {
                // logger.LogDebug(
                //     "Will schedule day {Date} using template {Template}",
                //     current,
                //     playoutTemplate.Template.Name);

                foreach (TemplateItem templateItem in playoutTemplate.Template.Items)
                {
                    var effectiveBlock = new EffectiveBlock(
                        templateItem.Block,
                        new BlockKey(templateItem.Block, templateItem.Template, playoutTemplate),
                        new DateTimeOffset(
                            current.Year,
                            current.Month,
                            current.Day,
                            templateItem.StartTime.Hours,
                            templateItem.StartTime.Minutes,
                            0,
                            start.Offset));

                    effectiveBlocks.Add(effectiveBlock);
                }

                current = current.AddDays(1);
            }
        }

        effectiveBlocks.RemoveAll(b => b.Start.AddMinutes(b.Block.Minutes) < start || b.Start > finish);
        effectiveBlocks = effectiveBlocks.OrderBy(rb => rb.Start).ToList();

        return effectiveBlocks;
    }
}
