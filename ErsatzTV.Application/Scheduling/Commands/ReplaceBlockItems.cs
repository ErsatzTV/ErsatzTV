using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record ReplaceBlockItems(int BlockId, string Name, int Minutes, List<ReplaceBlockItem> Items)
    : IRequest<Either<BaseError, List<BlockItemViewModel>>>;
