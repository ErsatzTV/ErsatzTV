using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddEpisodeToCollection(int CollectionId, int EpisodeId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
