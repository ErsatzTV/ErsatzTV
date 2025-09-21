using ErsatzTV.Core;

namespace ErsatzTV.Application.Subtitles;

public record ExtractEmbeddedShowSubtitles(int ShowId) : IBackgroundServiceRequest, IRequest<Option<BaseError>>;
