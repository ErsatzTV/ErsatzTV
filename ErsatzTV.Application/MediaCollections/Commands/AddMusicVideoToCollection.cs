using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddMusicVideoToCollection
        (int CollectionId, int MusicVideoId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
