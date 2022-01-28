using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Streaming.Queries;

public record GetLastPtsDuration(string FileName) : IRequest<Either<BaseError, PtsAndDuration>>;
