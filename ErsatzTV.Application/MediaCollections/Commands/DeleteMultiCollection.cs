using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections;

public record DeleteMultiCollection(int MultiCollectionId) : MediatR.IRequest<Either<BaseError, Unit>>;