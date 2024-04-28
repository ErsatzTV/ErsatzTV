using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record ReplacePlaylistItems(int PlaylistId, string Name, List<ReplacePlaylistItem> Items)
    : IRequest<Either<BaseError, List<PlaylistItemViewModel>>>;
