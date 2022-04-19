using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Channels;

public record UpdateChannel
(
    int ChannelId,
    string Name,
    string Number,
    string Group,
    string Categories,
    int FFmpegProfileId,
    string Logo,
    string PreferredAudioLanguageCode,
    StreamingMode StreamingMode,
    int? WatermarkId,
    int? FallbackFillerId,
    string PreferredSubtitleLanguageCode,
    ChannelSubtitleMode SubtitleMode) : IRequest<Either<BaseError, ChannelViewModel>>;
