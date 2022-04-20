using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegStreamSelector : IFFmpegStreamSelector
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly ILogger<FFmpegStreamSelector> _logger;
    private readonly ISearchRepository _searchRepository;

    public FFmpegStreamSelector(
        ISearchRepository searchRepository,
        ILogger<FFmpegStreamSelector> logger,
        IConfigElementRepository configElementRepository)
    {
        _searchRepository = searchRepository;
        _logger = logger;
        _configElementRepository = configElementRepository;
    }

    public Task<MediaStream> SelectVideoStream(Channel channel, MediaVersion version) =>
        version.Streams.First(s => s.MediaStreamKind == MediaStreamKind.Video).AsTask();

    public async Task<Option<MediaStream>> SelectAudioStream(Channel channel, MediaVersion version)
    {
        if (channel.StreamingMode == StreamingMode.HttpLiveStreamingDirect &&
            string.IsNullOrWhiteSpace(channel.PreferredAudioLanguageCode))
        {
            _logger.LogDebug(
                "Channel {Number} is HLS Direct with no preferred audio language; using all audio streams",
                channel.Number);
            return None;
        }

        var audioStreams = version.Streams.Filter(s => s.MediaStreamKind == MediaStreamKind.Audio).ToList();

        string language = (channel.PreferredAudioLanguageCode ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(language))
        {
            _logger.LogDebug("Channel {Number} has no preferred audio language code", channel.Number);
            Option<string> maybeDefaultLanguage = await _configElementRepository.GetValue<string>(
                ConfigElementKey.FFmpegPreferredLanguageCode);
            maybeDefaultLanguage.Match(
                lang => language = lang.ToLowerInvariant(),
                () =>
                {
                    _logger.LogDebug("FFmpeg has no preferred audio language code; falling back to {Code}", "eng");
                    language = "eng";
                });
        }

        List<string> allCodes = await _searchRepository.GetAllLanguageCodes(new List<string> { language });
        if (allCodes.Count > 1)
        {
            _logger.LogDebug("Preferred audio language has multiple codes {Codes}", allCodes);
        }

        var correctLanguage = audioStreams.Filter(
            s => allCodes.Any(
                c => string.Equals(
                    s.Language,
                    c,
                    StringComparison.InvariantCultureIgnoreCase))).ToList();
        if (correctLanguage.Any())
        {
            _logger.LogDebug(
                "Found {Count} audio streams with preferred audio language code(s) {Code}; selecting stream with most channels",
                correctLanguage.Count,
                allCodes);

            return correctLanguage.OrderByDescending(s => s.Channels).Head();
        }

        _logger.LogDebug(
            "Unable to find audio stream with preferred audio language code(s) {Code}; selecting stream with most channels",
            allCodes);

        return audioStreams.OrderByDescending(s => s.Channels).Head();
    }

    public async Task<Option<Subtitle>> SelectSubtitleStream(
        Channel channel,
        MediaVersion version,
        List<Subtitle> subtitles)
    {
        if (channel.SubtitleMode == ChannelSubtitleMode.None)
        {
            return None;
        }

        if (channel.StreamingMode == StreamingMode.HttpLiveStreamingDirect &&
            string.IsNullOrWhiteSpace(channel.PreferredSubtitleLanguageCode))
        {
            // _logger.LogDebug(
            //     "Channel {Number} is HLS Direct with no preferred subtitle language; using all subtitle streams",
            //     channel.Number);
            return None;
        }

        string language = (channel.PreferredSubtitleLanguageCode ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(language))
        {
            _logger.LogDebug("Channel {Number} has no preferred subtitle language code", channel.Number);
        }
        else
        {
            // filter to preferred language
            List<string> allCodes = await _searchRepository.GetAllLanguageCodes(new List<string> { language });
            subtitles = subtitles
                .Filter(
                    s => allCodes.Any(c => string.Equals(s.Language, c, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();
        }

        if (subtitles.Count > 0)
        {
            switch (channel.SubtitleMode)
            {
                case ChannelSubtitleMode.Forced:
                    foreach (Subtitle subtitle in subtitles.OrderBy(s => s.StreamIndex).Find(s => s.Forced))
                    {
                        return subtitle;
                    }

                    break;
                case ChannelSubtitleMode.Default:
                    foreach (Subtitle subtitle in subtitles.OrderBy(s => s.Default ? 0 : 1).ThenBy(s => s.StreamIndex))
                    {
                        return subtitle;
                    }

                    break;
                case ChannelSubtitleMode.Any:
                    foreach (Subtitle subtitle in subtitles.OrderBy(s => s.StreamIndex).HeadOrNone())
                    {
                        return subtitle;
                    }

                    break;
            }
        }

        _logger.LogDebug(
            "Found no subtitles for channel {ChannelNumber} with mode {Mode} matching language {Language}",
            channel.Number,
            channel.SubtitleMode,
            channel.PreferredSubtitleLanguageCode);

        return None;
    }
}
