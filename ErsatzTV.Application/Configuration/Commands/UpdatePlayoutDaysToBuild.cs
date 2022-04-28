using ErsatzTV.Core;

namespace ErsatzTV.Application.Configuration;

public record UpdatePlayoutDaysToBuild(int DaysToBuild) : IRequest<Either<BaseError, Unit>>;
