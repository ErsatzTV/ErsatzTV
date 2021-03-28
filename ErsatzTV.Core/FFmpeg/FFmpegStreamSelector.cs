using System;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegStreamSelector : IFFmpegStreamSelector
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly ILogger<FFmpegStreamSelector> _logger;

        public FFmpegStreamSelector(
            ILogger<FFmpegStreamSelector> logger,
            IConfigElementRepository configElementRepository)
        {
            _logger = logger;
            _configElementRepository = configElementRepository;
        }

        public Task<MediaStream> SelectVideoStream(Channel channel, MediaVersion version) =>
            version.Streams.First(s => s.MediaStreamKind == MediaStreamKind.Video).AsTask();

        public async Task<MediaStream> SelectAudioStream(Channel channel, MediaVersion version)
        {
            var audioStreams = version.Streams.Filter(s => s.MediaStreamKind == MediaStreamKind.Audio).ToList();

            string language = channel.PreferredLanguageCode.ToLowerInvariant();
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

            var correctLanguage = audioStreams.Filter(
                s => string.Equals(
                    s.Language,
                    language,
                    StringComparison.InvariantCultureIgnoreCase)).ToList();
            if (correctLanguage.Any())
            {
                _logger.LogDebug(
                    "Found {Count} audio streams with preferred language code {Code}; selecting stream with most channels",
                    correctLanguage.Count,
                    language);

                return correctLanguage.OrderByDescending(s => s.Channels).Head();
            }

            _logger.LogDebug(
                "Unable to find audio stream with preferred language code {Code}; selecting stream with most channels",
                language);

            return audioStreams.OrderByDescending(s => s.Channels).Head();
        }
    }
}
