using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

internal static class Mapper
{
    internal static BlockGroupViewModel ProjectToViewModel(BlockGroup blockGroup) =>
        new(blockGroup.Id, blockGroup.Name, blockGroup.Blocks.Count);

    internal static BlockViewModel ProjectToViewModel(Block block) =>
        new(block.Id, block.Name, block.Minutes);

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
        new(template.Id, template.Name);

    internal static TemplateItemViewModel ProjectToViewModel(TemplateItem templateItem) =>
        new(templateItem.Id, templateItem.BlockId, templateItem.Block.Name);
}
