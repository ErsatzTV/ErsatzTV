using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

internal static class Mapper
{
    internal static BlockGroupViewModel ProjectToViewModel(BlockGroup blockGroup) =>
        new(blockGroup.Id, blockGroup.Name, blockGroup.Blocks.Count);

    internal static BlockViewModel ProjectToViewModel(Block block) =>
        new(block.Id, block.Name);
}
