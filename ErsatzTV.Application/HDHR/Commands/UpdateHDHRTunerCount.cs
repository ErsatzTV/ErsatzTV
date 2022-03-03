using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.HDHR;

public record UpdateHDHRTunerCount(int TunerCount) : MediatR.IRequest<Either<BaseError, Unit>>;