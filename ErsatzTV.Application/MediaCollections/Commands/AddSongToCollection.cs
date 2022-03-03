using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections;

public record AddSongToCollection
    (int CollectionId, int SongId) : MediatR.IRequest<Either<BaseError, Unit>>;