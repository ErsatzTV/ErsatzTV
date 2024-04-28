using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record CreatePlaylist(int PlaylistGroupId, string Name) : IRequest<Either<BaseError, PlaylistViewModel>>;
