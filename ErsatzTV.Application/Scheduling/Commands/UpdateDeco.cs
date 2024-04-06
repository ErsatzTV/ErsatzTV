using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Application.Scheduling;

public record UpdateDeco(
    int DecoId,
    int DecoGroupId,
    string Name,
    DecoMode WatermarkMode,
    int? WatermarkId,
    DecoMode DeadAirFallbackMode,
    ProgramScheduleItemCollectionType DeadAirFallbackCollectionType,
    int? DeadAirFallbackCollectionId,
    int? DeadAirFallbackMediaItemId,
    int? DeadAirFallbackMultiCollectionId,
    int? DeadAirFallbackSmartCollectionId)
    : IRequest<Either<BaseError, DecoViewModel>>;
