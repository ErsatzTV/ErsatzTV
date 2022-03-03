using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections;

public record AddOtherVideoToCollection
    (int CollectionId, int OtherVideoId) : MediatR.IRequest<Either<BaseError, Unit>>;