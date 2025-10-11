using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record CopyBlock(int BlockId, int NewBlockGroupId, string NewBlockName)
    : IRequest<Either<BaseError, BlockViewModel>>;
