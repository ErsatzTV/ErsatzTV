using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections;

public record AddEpisodeToCollection(int CollectionId, int EpisodeId) : MediatR.IRequest<Either<BaseError, Unit>>;