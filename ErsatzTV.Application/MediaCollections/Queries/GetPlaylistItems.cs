namespace ErsatzTV.Application.MediaCollections;

public record GetPlaylistItems(int PlaylistId) : IRequest<List<PlaylistItemViewModel>>;
