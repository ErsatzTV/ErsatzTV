using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections;

public record AddShowToCollection(int CollectionId, int ShowId) : MediatR.IRequest<Either<BaseError, Unit>>;