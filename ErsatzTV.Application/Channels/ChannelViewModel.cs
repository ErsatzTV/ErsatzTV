using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Channels;

public record ChannelViewModel(
    int Id,
    string Number,
    string Name,
    string Group,
    string Categories,
    int FFmpegProfileId,
    string Logo,
    string PreferredLanguageCode,
    StreamingMode StreamingMode,
    int? WatermarkId,
    int? FallbackFillerId,
    int PlayoutCount);