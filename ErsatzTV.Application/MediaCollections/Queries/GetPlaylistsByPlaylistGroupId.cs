namespace ErsatzTV.Application.MediaCollections;

public record GetPlaylistsByPlaylistGroupId(int PlaylistGroupId) : IRequest<List<PlaylistViewModel>>;
