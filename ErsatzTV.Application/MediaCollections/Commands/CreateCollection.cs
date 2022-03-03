using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections;

public record CreateCollection(string Name) : IRequest<Either<BaseError, MediaCollectionViewModel>>;