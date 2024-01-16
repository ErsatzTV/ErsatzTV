using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

internal static class Mapper
{
    internal static BlockGroupViewModel ProjectToViewModel(BlockGroup blockGroup) =>
        new(blockGroup.Id, blockGroup.Name, blockGroup.Blocks.Count);

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
            blockItem.PlaybackOrder);

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

    internal static PlayoutTemplateViewModel ProjectToViewModel(PlayoutTemplate playoutTemplate) =>
        new(
            playoutTemplate.Id,
            ProjectToViewModel(playoutTemplate.Template),
            playoutTemplate.Index,
            playoutTemplate.DaysOfWeek,
            playoutTemplate.DaysOfMonth,
            playoutTemplate.MonthsOfYear);

    internal static PlayoutItemPreviewViewModel ProjectToViewModel(PlayoutItem playoutItem) =>
        new(
            Playouts.Mapper.GetDisplayTitle(playoutItem),
            playoutItem.StartOffset.TimeOfDay,
            playoutItem.FinishOffset.TimeOfDay,
            Playouts.Mapper.GetDisplayDuration(playoutItem.FinishOffset - playoutItem.StartOffset));
}
