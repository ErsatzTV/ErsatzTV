using ErsatzTV.Core;

namespace ErsatzTV.Application.HDHR;

public record UpdateHDHRTunerCount(int TunerCount) : MediatR.IRequest<Either<BaseError, Unit>>;