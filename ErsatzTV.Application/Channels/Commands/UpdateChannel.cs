using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Channels;

public record UpdateChannel(
    int ChannelId,
    string Name,
    string Number,
    string Group,
    string Categories,
    int FFmpegProfileId,
    string Logo,
    ChannelStreamSelectorMode StreamSelectorMode,
    string StreamSelector,
    string PreferredAudioLanguageCode,
    string PreferredAudioTitle,
    ChannelProgressMode ProgressMode,
    StreamingMode StreamingMode,
    int? WatermarkId,
    int? FallbackFillerId,
    string PreferredSubtitleLanguageCode,
    ChannelSubtitleMode SubtitleMode,
    ChannelMusicVideoCreditsMode MusicVideoCreditsMode,
    string MusicVideoCreditsTemplate,
    ChannelSongVideoMode SongVideoMode,
    ChannelActiveMode ActiveMode) : IRequest<Either<BaseError, ChannelViewModel>>;
