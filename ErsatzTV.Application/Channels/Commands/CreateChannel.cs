using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Channels;

public record CreateChannel
(
    string Name,
    string Number,
    string Group,
    string Categories,
    int FFmpegProfileId,
    string Logo,
    string PreferredLanguageCode,
    StreamingMode StreamingMode,
    int? WatermarkId,
    int? FallbackFillerId) : IRequest<Either<BaseError, CreateChannelResult>>;