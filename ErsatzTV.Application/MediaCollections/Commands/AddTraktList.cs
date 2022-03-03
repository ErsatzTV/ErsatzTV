using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaCollections;

public record AddTraktList(string TraktListUrl) : IRequest<Either<BaseError, Unit>>, IBackgroundServiceRequest;