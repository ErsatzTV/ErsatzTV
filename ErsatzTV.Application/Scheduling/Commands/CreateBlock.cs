using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record CreateBlock(int BlockGroupId, string Name) : IRequest<Either<BaseError, BlockViewModel>>;
