using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Streaming;

public record GetLastPtsDuration(string FileName) : IRequest<Either<BaseError, PtsAndDuration>>;
