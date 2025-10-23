using ErsatzTV.Application.Scheduling;
using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record PreviewPlaylistPlayout(ReplacePlaylistItems Data)
    : IRequest<Either<BaseError, List<PlayoutItemPreviewViewModel>>>;
