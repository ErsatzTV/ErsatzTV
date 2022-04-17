using ErsatzTV.Core;

namespace ErsatzTV.Application.Maintenance;

public record EmptyTrash : IRequest<Either<BaseError, Unit>>;
