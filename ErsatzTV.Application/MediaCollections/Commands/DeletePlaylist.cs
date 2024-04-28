using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record DeletePlaylist(int PlaylistId) : IRequest<Option<BaseError>>;
