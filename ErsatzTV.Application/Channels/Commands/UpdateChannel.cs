using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

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
    string PreferredLanguageCode,
    StreamingMode StreamingMode,
    int? WatermarkId,
    int? FallbackFillerId) : IRequest<Either<BaseError, ChannelViewModel>>;