using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddSongToCollection
        (int CollectionId, int SongId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
