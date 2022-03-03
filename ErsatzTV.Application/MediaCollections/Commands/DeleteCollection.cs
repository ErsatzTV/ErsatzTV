using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections;

public record DeleteCollection(int CollectionId) : IRequest<Either<BaseError, LanguageExt.Unit>>;