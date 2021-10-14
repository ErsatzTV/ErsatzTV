using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddOtherVideoToCollection
        (int CollectionId, int OtherVideoId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
