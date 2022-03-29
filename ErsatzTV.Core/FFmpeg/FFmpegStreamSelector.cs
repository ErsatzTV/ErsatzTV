using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegStreamSelector : IFFmpegStreamSelector
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly ISearchRepository _searchRepository;
    private readonly ILogger<FFmpegStreamSelector> _logger;

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
            string.IsNullOrWhiteSpace(channel.PreferredLanguageCode))
        {
            _logger.LogDebug(
                "Channel {Number} is HLS Direct with no preferred language; using all audio streams",
                channel.Number);
            return None;
        }

        var audioStreams = version.Streams.Filter(s => s.MediaStreamKind == MediaStreamKind.Audio).ToList();

        string language = (channel.PreferredLanguageCode ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(language))
        {
            _logger.LogDebug("Channel {Number} has no preferred language code", channel.Number);
            Option<string> maybeDefaultLanguage = await _configElementRepository.GetValue<string>(
                ConfigElementKey.FFmpegPreferredLanguageCode);
            maybeDefaultLanguage.Match(
                lang => language = lang.ToLowerInvariant(),
                () =>
                {
                    _logger.LogDebug("FFmpeg has no preferred language code; falling back to {Code}", "eng");
                    language = "eng";
                });
        }

        List<string> allCodes = await _searchRepository.GetAllLanguageCodes(new List<string> { language });
        if (allCodes.Count > 1)
        {
            _logger.LogDebug("Preferred language has multiple codes {Codes}", allCodes);
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
                "Found {Count} audio streams with preferred language code(s) {Code}; selecting stream with most channels",
                correctLanguage.Count,
                allCodes);

            return correctLanguage.OrderByDescending(s => s.Channels).Head();
        }

        _logger.LogDebug(
            "Unable to find audio stream with preferred language code(s) {Code}; selecting stream with most channels",
            allCodes);

        return audioStreams.OrderByDescending(s => s.Channels).Head();
    }

    public async Task<Option<MediaStream>> SelectSubtitleStream(
        Channel channel,
        MediaVersion version,
        Option<MediaStream> audioStream)
    {
        if (channel.FFmpegProfile.SubtitleMode == FFmpegProfileSubtitleMode.None)
        {
            return None;
        }

        if (channel.StreamingMode == StreamingMode.HttpLiveStreamingDirect &&
            string.IsNullOrWhiteSpace(channel.PreferredLanguageCode))
        {
            _logger.LogDebug(
                "Channel {Number} is HLS Direct with no preferred language; using all subtitle streams",
                channel.Number);
            return None;
        }

        if (audioStream.IsNone)
        {
            _logger.LogDebug("Unable to determine audio language; using no subtitles");
            return None;
        }

        foreach (MediaStream stream in audioStream)
        {
            string language = (channel.PreferredLanguageCode ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(language))
            {
                _logger.LogDebug("Channel {Number} has no preferred language code", channel.Number);
                Option<string> maybeDefaultLanguage = await _configElementRepository.GetValue<string>(
                    ConfigElementKey.FFmpegPreferredLanguageCode);
                maybeDefaultLanguage.Match(
                    lang => language = lang.ToLowerInvariant(),
                    () =>
                    {
                        _logger.LogDebug("FFmpeg has no preferred language code; falling back to {Code}", "eng");
                        language = "eng";
                    });
            }

            List<string> allCodes = await _searchRepository.GetAllLanguageCodes(new List<string> { language });

            var subtitleStreams = version.Streams
                .Filter(s => s.MediaStreamKind == MediaStreamKind.Subtitle)
                .Filter(
                    s => allCodes.Any(c => string.Equals(s.Language, c, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();

            var forced = new List<MediaStream>();
            var defaults = new List<MediaStream>();

            if (allCodes.Any(c => string.Equals(stream.Language, c, StringComparison.InvariantCultureIgnoreCase)))
            {
                // audio stream uses desired language, only check forced subtitles
                _logger.LogDebug("Audio stream uses preferred language; checking for forced subtitles");
                forced.AddRange(subtitleStreams.Filter(s => s.Forced));
            }
            else
            {
                _logger.LogDebug("Audio stream does NOT use preferred language; checking for any subtitles");
                defaults.AddRange(subtitleStreams.Filter(s => s.Default));
            }

            foreach (MediaStream f in forced.HeadOrNone())
            {
                _logger.LogDebug("Found forced subtitle");
                return f;
            }

            foreach (MediaStream d in defaults.HeadOrNone())
            {
                _logger.LogDebug("Found default subtitle");
                return d;
            }

            foreach (MediaStream s in subtitleStreams.HeadOrNone())
            {
                _logger.LogDebug("Found subtitle");
                return s;
            }
        }

        return None;
    }
}