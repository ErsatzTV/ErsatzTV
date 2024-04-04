using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record UpdateDeco(int DecoId, int DecoGroupId, string Name, int? WatermarkId) : IRequest<Either<BaseError, DecoViewModel>>;
