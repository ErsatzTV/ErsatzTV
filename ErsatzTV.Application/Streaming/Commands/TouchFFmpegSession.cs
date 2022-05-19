using ErsatzTV.Core;

namespace ErsatzTV.Application.Streaming;

public record TouchFFmpegSession(string Path) : IRequest<Either<BaseError, Unit>>, IFFmpegWorkerRequest;
