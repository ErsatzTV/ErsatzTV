using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record CreateBlockGroup(string Name) : IRequest<Either<BaseError, BlockGroupViewModel>>;
