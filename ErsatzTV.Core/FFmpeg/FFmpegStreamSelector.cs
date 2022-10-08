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
        string preferredAudioLanguage,
        string preferredAudioTitle)
    {
        if (streamingMode == StreamingMode.HttpLiveStreamingDirect &&
            string.IsNullOrWhiteSpace(preferredAudioLanguage) && string.IsNullOrWhiteSpace(preferredAudioTitle))
        {
            _logger.LogDebug(
                "Channel {Number} is HLS Direct with no preferred audio language or title; using all audio streams",
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
                "Found {Count} audio streams with preferred audio language code(s) {Code}",
                correctLanguage.Count,
                allCodes);

            return PrioritizeAudioTitle(correctLanguage, preferredAudioTitle ?? string.Empty);
        }

        _logger.LogDebug(
            "Unable to find audio stream with preferred audio language code(s) {Code}",
            allCodes);

        return PrioritizeAudioTitle(audioStreams, preferredAudioTitle ?? string.Empty);
    }

    public async Task<Option<Subtitle>> SelectSubtitleStream(
        List<Subtitle> subtitles,
        Channel channel,
        string preferredSubtitleLanguage,
        ChannelSubtitleMode subtitleMode)
    {
        if (channel.MusicVideoCreditsMode is ChannelMusicVideoCreditsMode.GenerateSubtitles
                or ChannelMusicVideoCreditsMode.TemplateSubtitles &&
            subtitles.FirstOrDefault(s => s.SubtitleKind == SubtitleKind.Generated) is { } generatedSubtitle)
        {
            _logger.LogDebug("Selecting generated subtitle for channel {Number}", channel.Number);
            return Optional(generatedSubtitle);
        }

        if (subtitleMode == ChannelSubtitleMode.None)
        {
            return None;
        }

        if (channel.StreamingMode == StreamingMode.HttpLiveStreamingDirect &&
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
            channel.Number,
            subtitleMode,
            preferredSubtitleLanguage);

        return None;
    }

    private Option<MediaStream> PrioritizeAudioTitle(IReadOnlyCollection<MediaStream> streams, string title)
    {
        // return correctLanguage.OrderByDescending(s => s.Channels).Head();
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogDebug("No audio title has been specified; selecting stream with most channels");
            return streams.OrderByDescending(s => s.Channels).Head();
        }

        // prioritize matching titles
        var matchingTitle = streams
            .Filter(ms => (ms.Title ?? string.Empty).Contains(title, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matchingTitle.Any())
        {
            _logger.LogDebug(
                "Found {Count} audio streams with preferred title {Title}; selecting stream with most channels",
                matchingTitle.Count,
                title);

            return matchingTitle.OrderByDescending(s => s.Channels).Head();
        }

        _logger.LogDebug(
            "Unable to find audio stream with preferred title {Title}; selecting stream with most channels",
            title);

        return streams.OrderByDescending(s => s.Channels).Head();
    }
}
