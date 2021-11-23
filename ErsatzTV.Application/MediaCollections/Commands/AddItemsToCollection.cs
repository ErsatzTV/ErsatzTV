using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddItemsToCollection
    (
        int CollectionId,
        List<int> MovieIds,
        List<int> ShowIds,
        List<int> SeasonIds,
        List<int> EpisodeIds,
        List<int> ArtistIds,
        List<int> MusicVideoIds,
        List<int> OtherVideoIds,
        List<int> SongIds) : MediatR.IRequest<Either<BaseError, Unit>>;
}
