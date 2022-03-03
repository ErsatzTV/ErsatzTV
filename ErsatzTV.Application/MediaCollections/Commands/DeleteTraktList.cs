using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections;

public record DeleteTraktList(int TraktListId) : IRequest<Either<BaseError, LanguageExt.Unit>>,
    IBackgroundServiceRequest;