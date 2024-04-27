namespace ErsatzTV.Application.MediaCollections;

public record GetPlaylistById(int PlaylistId) : IRequest<Option<PlaylistViewModel>>;
