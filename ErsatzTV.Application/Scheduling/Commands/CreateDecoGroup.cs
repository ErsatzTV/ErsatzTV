using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record CreateDecoGroup(string Name) : IRequest<Either<BaseError, DecoGroupViewModel>>;
