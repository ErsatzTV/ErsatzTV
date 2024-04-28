using ErsatzTV.Application.Scheduling;

namespace ErsatzTV.Application.MediaCollections;

public record PreviewPlaylistPlayout(ReplacePlaylistItems Data) : IRequest<List<PlayoutItemPreviewViewModel>>;
