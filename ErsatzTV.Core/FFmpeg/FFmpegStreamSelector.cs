using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

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
                "Channel {Number} is HLS with no preferred language; using all audio streams",
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
}