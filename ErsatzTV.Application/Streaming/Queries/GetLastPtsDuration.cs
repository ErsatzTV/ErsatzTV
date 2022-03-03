using ErsatzTV.Core;

namespace ErsatzTV.Application.Streaming;

public record GetLastPtsDuration(string FileName) : IRequest<Either<BaseError, PtsAndDuration>>;
