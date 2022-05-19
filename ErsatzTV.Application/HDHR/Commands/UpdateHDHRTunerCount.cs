using ErsatzTV.Core;

namespace ErsatzTV.Application.HDHR;

public record UpdateHDHRTunerCount(int TunerCount) : IRequest<Either<BaseError, Unit>>;
