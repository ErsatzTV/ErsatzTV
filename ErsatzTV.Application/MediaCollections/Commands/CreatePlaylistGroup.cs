using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record CreatePlaylistGroup(string Name) : IRequest<Either<BaseError, PlaylistGroupViewModel>>;
