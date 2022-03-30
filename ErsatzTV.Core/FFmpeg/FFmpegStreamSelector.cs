﻿using ErsatzTV.Core.Domain;
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

    public async Task<Option<MediaStream>> SelectSubtitleStream(Channel channel, MediaVersion version)
    {
        if (channel.SubtitleMode == ChannelSubtitleMode.None)
        {
            return None;
        }
        
        if (channel.StreamingMode == StreamingMode.HttpLiveStreamingDirect &&
            string.IsNullOrWhiteSpace(channel.PreferredSubtitleLanguageCode))
        {
            _logger.LogDebug(
                "Channel {Number} is HLS Direct with no preferred subtitle language; using all subtitle streams",
                channel.Number);
            return None;
        }

        var subtitleStreams = version.Streams
            .Filter(s => s.MediaStreamKind == MediaStreamKind.Subtitle)
            .Filter(s => s.Codec is "hdmv_pgs_subtitle" or "dvd_subtitle")
            .ToList();

        string language = (channel.PreferredSubtitleLanguageCode ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(language))
        {
            _logger.LogDebug("Channel {Number} has no preferred subtitle language code", channel.Number);
        }
        else
        {
            // filter to preferred language
            List<string> allCodes = await _searchRepository.GetAllLanguageCodes(new List<string> { language });
            subtitleStreams = version.Streams
                .Filter(
                    s => allCodes.Any(c => string.Equals(s.Language, c, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();
        }

        if (subtitleStreams.Count == 0)
        {
            return None;
        }

        switch (channel.SubtitleMode)
        {
            case ChannelSubtitleMode.Forced:
                foreach (MediaStream stream in Optional(subtitleStreams.OrderBy(s => s.Index).Find(s => s.Forced)))
                {
                    return stream;
                }
                break;
            case ChannelSubtitleMode.Default:
                foreach (MediaStream stream in Optional(subtitleStreams.OrderBy(s => s.Index).Find(s => s.Default)))
                {
                    return stream;
                }
                break;
            case ChannelSubtitleMode.Any:
                foreach (MediaStream stream in subtitleStreams.OrderBy(s => s.Index).HeadOrNone())
                {
                    return stream;
                }
                break;
        }

        return None;
    }
}