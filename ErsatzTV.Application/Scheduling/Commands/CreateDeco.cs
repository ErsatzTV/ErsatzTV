using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record CreateDeco(int DecoGroupId, string Name) : IRequest<Either<BaseError, DecoViewModel>>;
