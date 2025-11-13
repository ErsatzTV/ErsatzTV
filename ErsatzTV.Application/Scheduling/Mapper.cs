using ErsatzTV.Application.Tree;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

internal static class Mapper
{
    internal static TreeViewModel ProjectToViewModel(List<DecoTemplateGroup> decoTemplateGroups) =>
        new(
            decoTemplateGroups.OrderBy(dtg => dtg.Name).Map(dtg => new TreeGroupViewModel(
                    dtg.Id,
                    dtg.Name,
                    dtg.DecoTemplates.OrderBy(dt => dt.Name).Map(dt => new TreeItemViewModel(dt.Id, dt.Name)).ToList()))
                .ToList());

    internal static TreeViewModel ProjectToViewModel(List<DecoGroup> decoGroups) =>
        new(
            decoGroups.OrderBy(dg => dg.Name).Map(dg => new TreeGroupViewModel(
                dg.Id,
                dg.Name,
                dg.Decos.OrderBy(d => d.Name).Map(d => new TreeItemViewModel(d.Id, d.Name)).ToList())).ToList());

    internal static TreeViewModel ProjectToViewModel(List<TemplateGroup> templateGroups) =>
        new(
            templateGroups.OrderBy(tg => tg.Name).Map(tg => new TreeGroupViewModel(
                tg.Id,
                tg.Name,
                tg.Templates.OrderBy(t => t.Name).Map(t => new TreeItemViewModel(t.Id, t.Name)).ToList())).ToList());

    internal static BlockGroupViewModel ProjectToViewModel(BlockGroup blockGroup) =>
        new(blockGroup.Id, blockGroup.Name);

    internal static BlockViewModel ProjectToViewModel(Block block) =>
        new(block.Id, block.BlockGroupId, block.BlockGroup.Name, block.Name, block.Minutes, block.StopScheduling);

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
            blockItem.SearchTitle,
            blockItem.SearchQuery,
            blockItem.PlaybackOrder,
            blockItem.IncludeInProgramGuide,
            blockItem.DisableWatermarks,
            blockItem.BlockItemWatermarks.Map(wm => Watermarks.Mapper.ProjectToViewModel(wm.Watermark)).ToList(),
            blockItem.BlockItemGraphicsElements.Map(ge => Graphics.Mapper.ProjectToViewModel(ge.GraphicsElement)).ToList());

    internal static TemplateGroupViewModel ProjectToViewModel(TemplateGroup templateGroup) =>
        new(templateGroup.Id, templateGroup.Name, templateGroup.Templates.Count);

    internal static TemplateViewModel ProjectToViewModel(Template template) =>
        new(template.Id, template.TemplateGroupId, template.TemplateGroup.Name, template.Name);

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
            deco.DecoGroup.Name,
            deco.Name,
            deco.WatermarkMode,
            deco.DecoWatermarks.Map(wm => Watermarks.Mapper.ProjectToViewModel(wm.Watermark)).ToList(),
            deco.UseWatermarkDuringFiller,
            deco.GraphicsElementsMode,
            deco.DecoGraphicsElements.Map(ge => Graphics.Mapper.ProjectToViewModel(ge.GraphicsElement)).ToList(),
            deco.UseGraphicsElementsDuringFiller,
            deco.BreakContentMode,
            deco.BreakContent.Map(ProjectToViewModel).ToList(),
            deco.DefaultFillerMode,
            deco.DefaultFillerCollectionType,
            deco.DefaultFillerCollection is not null
                ? MediaCollections.Mapper.ProjectToViewModel(deco.DefaultFillerCollection)
                : null,
            deco.DefaultFillerMediaItem switch
            {
                Show show => MediaItems.Mapper.ProjectToViewModel(show),
                Season season => MediaItems.Mapper.ProjectToViewModel(season),
                Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                _ => null
            },
            deco.DefaultFillerMultiCollection is not null
                ? MediaCollections.Mapper.ProjectToViewModel(deco.DefaultFillerMultiCollection)
                : null,
            deco.DefaultFillerSmartCollection is not null
                ? MediaCollections.Mapper.ProjectToViewModel(deco.DefaultFillerSmartCollection)
                : null,
            deco.DefaultFillerTrimToFit,
            deco.DeadAirFallbackMode,
            deco.DeadAirFallbackCollectionType,
            deco.DeadAirFallbackCollection is not null
                ? MediaCollections.Mapper.ProjectToViewModel(deco.DeadAirFallbackCollection)
                : null,
            deco.DeadAirFallbackMediaItem switch
            {
                Show show => MediaItems.Mapper.ProjectToViewModel(show),
                Season season => MediaItems.Mapper.ProjectToViewModel(season),
                Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                _ => null
            },
            deco.DeadAirFallbackMultiCollection is not null
                ? MediaCollections.Mapper.ProjectToViewModel(deco.DeadAirFallbackMultiCollection)
                : null,
            deco.DeadAirFallbackSmartCollection is not null
                ? MediaCollections.Mapper.ProjectToViewModel(deco.DeadAirFallbackSmartCollection)
                : null);

    internal static DecoBreakContentViewModel ProjectToViewModel(DecoBreakContent decoBreakContent) =>
        new(
            decoBreakContent.Id,
            decoBreakContent.CollectionType,
            decoBreakContent.Collection is not null
                ? MediaCollections.Mapper.ProjectToViewModel(decoBreakContent.Collection)
                : null,
            decoBreakContent.MediaItem switch
            {
                Show show => MediaItems.Mapper.ProjectToViewModel(show),
                Season season => MediaItems.Mapper.ProjectToViewModel(season),
                Artist artist => MediaItems.Mapper.ProjectToViewModel(artist),
                _ => null
            },
            decoBreakContent.MultiCollection is not null
                ? MediaCollections.Mapper.ProjectToViewModel(decoBreakContent.MultiCollection)
                : null,
            decoBreakContent.SmartCollection is not null
                ? MediaCollections.Mapper.ProjectToViewModel(decoBreakContent.SmartCollection)
                : null,
            decoBreakContent.Playlist is not null
                ? MediaCollections.Mapper.ProjectToViewModel(decoBreakContent.Playlist)
                : null,
            decoBreakContent.Placement);

    internal static DecoTemplateGroupViewModel ProjectToViewModel(DecoTemplateGroup decoTemplateGroup) =>
        new(decoTemplateGroup.Id, decoTemplateGroup.Name, decoTemplateGroup.DecoTemplates.Count);

    internal static DecoTemplateViewModel ProjectToViewModel(DecoTemplate decoTemplate)
    {
        if (decoTemplate is null)
        {
            return null;
        }

        return new DecoTemplateViewModel(
            decoTemplate.Id,
            decoTemplate.DecoTemplateGroupId,
            decoTemplate.DecoTemplateGroup.Name,
            decoTemplate.Name);
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
            Playouts.Mapper.GetDisplayTitle(playoutItem.MediaItem, playoutItem.ChapterTitle),
            playoutItem.StartOffset.TimeOfDay,
            playoutItem.FinishOffset.TimeOfDay,
            playoutItem.GetDisplayDuration());
}
