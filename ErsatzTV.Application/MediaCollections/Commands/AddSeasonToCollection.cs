using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddSeasonToCollection(int CollectionId, int SeasonId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
