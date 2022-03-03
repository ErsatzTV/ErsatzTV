using ErsatzTV.Core;

namespace ErsatzTV.Application.Maintenance;

public record DeleteItemsFromDatabase(List<int> MediaItemIds) : IRequest<Either<BaseError, Unit>>;
