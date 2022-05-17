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

    public Task<MediaStream> SelectVideoStream(MediaVersion version) =>
        version.Streams.First(s => s.MediaStreamKind == MediaStreamKind.Video).AsTask();

    public async Task<Option<MediaStream>> SelectAudioStream(
        MediaVersion version,
        StreamingMode streamingMode,
        string channelNumber,
        string preferredAudioLanguage)
    {
        if (streamingMode == StreamingMode.HttpLiveStreamingDirect &&
            string.IsNullOrWhiteSpace(preferredAudioLanguage))
        {
            _logger.LogDebug(
                "Channel {Number} is HLS Direct with no preferred audio language; using all audio streams",
                channelNumber);
            return None;
        }

        var audioStreams = version.Streams.Filter(s => s.MediaStreamKind == MediaStreamKind.Audio).ToList();

        string language = (preferredAudioLanguage ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(language))
        {
            _logger.LogDebug("Channel {Number} has no preferred audio language code", channelNumber);
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

        return audioStreams.OrderByDescending(s => s.Channels).HeadOrNone();
    }

    public async Task<Option<Subtitle>> SelectSubtitleStream(
        MediaVersion version,
        List<Subtitle> subtitles,
        StreamingMode streamingMode,
        string channelNumber,
        string preferredSubtitleLanguage,
        ChannelSubtitleMode subtitleMode)
    {
        if (subtitleMode == ChannelSubtitleMode.None)
        {
            return None;
        }

        if (streamingMode == StreamingMode.HttpLiveStreamingDirect &&
            string.IsNullOrWhiteSpace(preferredSubtitleLanguage))
        {
            // _logger.LogDebug(
            //     "Channel {Number} is HLS Direct with no preferred subtitle language; using all subtitle streams",
            //     channel.Number);
            return None;
        }

        string language = (preferredSubtitleLanguage ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(language))
        {
            _logger.LogDebug("Channel {Number} has no preferred subtitle language code", channelNumber);
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
            switch (subtitleMode)
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
            channelNumber,
            subtitleMode,
            preferredSubtitleLanguage);

        return None;
    }
}
