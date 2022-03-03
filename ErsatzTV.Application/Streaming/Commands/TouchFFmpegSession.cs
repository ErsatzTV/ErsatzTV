using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Streaming;

public record TouchFFmpegSession(string Path) : IRequest<Either<BaseError, Unit>>, IFFmpegWorkerRequest;