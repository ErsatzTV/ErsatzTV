using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record DeletePlaylistGroup(int PlaylistGroupId) : IRequest<Option<BaseError>>;
