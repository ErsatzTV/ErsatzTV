using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

public record ReplaceBlockItems(
    int BlockId,
    string Name,
    int Minutes,
    BlockStopScheduling StopScheduling,
    List<ReplaceBlockItem> Items)
    : IRequest<Either<BaseError, List<BlockItemViewModel>>>;
