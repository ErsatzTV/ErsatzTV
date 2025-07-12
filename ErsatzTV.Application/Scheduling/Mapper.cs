using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

internal static class Mapper
{
    internal static BlockTreeViewModel ProjectToViewModel(List<BlockGroup> blockGroups) =>
        new(
            blockGroups.Map(bg => new BlockTreeBlockGroupViewModel(
                bg.Id,
                bg.Name,
                bg.Blocks.Map(b => new BlockTreeBlockViewModel(b.Id, b.Name, b.Minutes)).ToList())).ToList());

    internal static BlockGroupViewModel ProjectToViewModel(BlockGroup blockGroup) =>
        new(blockGroup.Id, blockGroup.Name);

    internal static BlockViewModel ProjectToViewModel(Block block) =>
        new(block.Id, block.Name, block.Minutes, block.StopScheduling);

    internal static BlockItemViewModel ProjectToViewModel(BlockItem blockItem) =>
        new(
            blockItem.Id,
            blockItem.Index,
            blockItem.CollectionType,
            blockItem.Collection is not null ? MediaCollections.Mapper.ProjectToViewModel(blockItem.Collection) : null,
            blockItem.MultiCollection is not null
                ? MediaCollections.Mapper.ProjectToViewModel(blockItem.MultiCollection)
                : null,
            blockItem.SmartCollection is not null
                ? MediaCollections.Mapper.ProjectToViewModel(blockItem.SmartCollection)
                : null,
            blockItem.MediaItem switch
            {
                Show show => MediaItems.Mapper.ProjectToViewModel(show),
                Season season => MediaItems.Mapper.ProjectToViewModel(season),
                Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                _ => null
            },
            blockItem.PlaybackOrder,
            blockItem.IncludeInProgramGuide,
            blockItem.DisableWatermarks);

    internal static TemplateGroupViewModel ProjectToViewModel(TemplateGroup templateGroup) =>
        new(templateGroup.Id, templateGroup.Name, templateGroup.Templates.Count);

    internal static TemplateViewModel ProjectToViewModel(Template template) =>
        new(template.Id, template.TemplateGroupId, template.Name);

    internal static TemplateItemViewModel ProjectToViewModel(TemplateItem templateItem)
    {
        DateTime startTime = DateTime.Today.Add(templateItem.StartTime);
        DateTime endTime = startTime.AddMinutes(templateItem.Block.Minutes);
        return new TemplateItemViewModel(templateItem.BlockId, templateItem.Block.Name, startTime, endTime);
    }

    internal static DecoGroupViewModel ProjectToViewModel(DecoGroup decoGroup) =>
        new(decoGroup.Id, decoGroup.Name, decoGroup.Decos.Count);

    internal static DecoViewModel ProjectToViewModel(Deco deco) =>
        new(
            deco.Id,
            deco.DecoGroupId,
            deco.Name,
            deco.WatermarkMode,
            deco.WatermarkId,
            deco.UseWatermarkDuringFiller,
            deco.DefaultFillerMode,
            deco.DefaultFillerCollectionType,
            deco.DefaultFillerCollectionId,
            deco.DefaultFillerMediaItemId,
            deco.DefaultFillerMultiCollectionId,
            deco.DefaultFillerSmartCollectionId,
            deco.DefaultFillerTrimToFit,
            deco.DeadAirFallbackMode,
            deco.DeadAirFallbackCollectionType,
            deco.DeadAirFallbackCollectionId,
            deco.DeadAirFallbackMediaItemId,
            deco.DeadAirFallbackMultiCollectionId,
            deco.DeadAirFallbackSmartCollectionId);

    internal static DecoTemplateGroupViewModel ProjectToViewModel(DecoTemplateGroup decoTemplateGroup) =>
        new(decoTemplateGroup.Id, decoTemplateGroup.Name, decoTemplateGroup.DecoTemplates.Count);

    internal static DecoTemplateViewModel ProjectToViewModel(DecoTemplate decoTemplate)
    {
        if (decoTemplate is null)
        {
            return null;
        }

        return new DecoTemplateViewModel(decoTemplate.Id, decoTemplate.DecoTemplateGroupId, decoTemplate.Name);
    }

    internal static DecoTemplateItemViewModel ProjectToViewModel(DecoTemplateItem decoTemplateItem)
    {
        DateTime startTime = DateTime.Today.Add(decoTemplateItem.StartTime);
        DateTime endTime = DateTime.Today.Add(decoTemplateItem.EndTime);
        if (startTime > endTime)
        {
            endTime = endTime.AddDays(1);
        }

        return new DecoTemplateItemViewModel(decoTemplateItem.DecoId, decoTemplateItem.Deco.Name, startTime, endTime);
    }

    internal static PlayoutTemplateViewModel ProjectToViewModel(PlayoutTemplate playoutTemplate) =>
        new(
            playoutTemplate.Id,
            ProjectToViewModel(playoutTemplate.Template),
            ProjectToViewModel(playoutTemplate.DecoTemplate),
            playoutTemplate.Index,
            playoutTemplate.DaysOfWeek,
            playoutTemplate.DaysOfMonth,
            playoutTemplate.MonthsOfYear,
            playoutTemplate.LimitToDateRange,
            playoutTemplate.StartMonth,
            playoutTemplate.StartDay,
            playoutTemplate.EndMonth,
            playoutTemplate.EndDay);

    internal static PlayoutItemPreviewViewModel ProjectToViewModel(PlayoutItem playoutItem) =>
        new(
            Playouts.Mapper.GetDisplayTitle(playoutItem),
            playoutItem.StartOffset.TimeOfDay,
            playoutItem.FinishOffset.TimeOfDay,
            playoutItem.GetDisplayDuration());
}
