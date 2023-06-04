using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scripting;
using ErsatzTV.Core.Scripting;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegStreamSelector : IFFmpegStreamSelector
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<FFmpegStreamSelector> _logger;
    private readonly IScriptEngine _scriptEngine;
    private readonly ISearchRepository _searchRepository;
    private readonly IStreamSelectorRepository _streamSelectorRepository;

    public FFmpegStreamSelector(
        IScriptEngine scriptEngine,
        IStreamSelectorRepository streamSelectorRepository,
        ISearchRepository searchRepository,
        IConfigElementRepository configElementRepository,
        ILocalFileSystem localFileSystem,
        ILogger<FFmpegStreamSelector> logger)
    {
        _scriptEngine = scriptEngine;
        _streamSelectorRepository = streamSelectorRepository;
        _searchRepository = searchRepository;
        _configElementRepository = configElementRepository;
        _localFileSystem = localFileSystem;
        _logger = logger;
    }

    public Task<MediaStream> SelectVideoStream(MediaVersion version) =>
        version.Streams.First(s => s.MediaStreamKind == MediaStreamKind.Video).AsTask();

    public async Task<Option<MediaStream>> SelectAudioStream(
        MediaItemAudioVersion version,
        StreamingMode streamingMode,
        Channel channel,
        string preferredAudioLanguage,
        string preferredAudioTitle)
    {
        if (streamingMode == StreamingMode.HttpLiveStreamingDirect &&
            string.IsNullOrWhiteSpace(preferredAudioLanguage) && string.IsNullOrWhiteSpace(preferredAudioTitle))
        {
            _logger.LogDebug(
                "Channel {Number} is HLS Direct with no preferred audio language or title; using all audio streams",
                channel.Number);
            return None;
        }

        string language = (preferredAudioLanguage ?? string.Empty).ToLowerInvariant();
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

        List<string> allLanguageCodes = await _searchRepository.GetAllLanguageCodes(new List<string> { language });
        if (allLanguageCodes.Count > 1)
        {
            _logger.LogDebug("Preferred audio language has multiple codes {Codes}", allLanguageCodes);
        }

        try
        {
            switch (version.MediaItem)
            {
                case Episode:
                    var sw = Stopwatch.StartNew();
                    Option<MediaStream> result = await SelectEpisodeAudioStream(
                        channel,
                        allLanguageCodes,
                        version.MediaItem.Id,
                        version.MediaVersion);
                    sw.Stop();
                    _logger.LogDebug("SelectAudioStream duration: {Duration}", sw.Elapsed);
                    if (result.IsSome)
                    {
                        return result;
                    }

                    break;
                case Movie:
                    var sw2 = Stopwatch.StartNew();
                    Option<MediaStream> result2 = await SelectMovieAudioStream(
                        channel,
                        allLanguageCodes,
                        version.MediaItem.Id,
                        version.MediaVersion);
                    sw2.Stop();
                    _logger.LogDebug("SelectAudioStream duration: {Duration}", sw2.Elapsed);
                    if (result2.IsSome)
                    {
                        return result2;
                    }

                    break;
                // let default fall through
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute audio stream selector script; falling back to built-in logic");
        }

        return DefaultSelectAudioStream(version.MediaVersion, allLanguageCodes, preferredAudioTitle);
    }

    public async Task<Option<Subtitle>> SelectSubtitleStream(
        List<Subtitle> subtitles,
        Channel channel,
        string preferredSubtitleLanguage,
        ChannelSubtitleMode subtitleMode)
    {
        if (channel.MusicVideoCreditsMode is ChannelMusicVideoCreditsMode.GenerateSubtitles &&
            subtitles.FirstOrDefault(s => s.SubtitleKind == SubtitleKind.Generated) is { } generatedSubtitle)
        {
            _logger.LogDebug("Selecting generated subtitle for channel {Number}", channel.Number);
            return Optional(generatedSubtitle);
        }

        if (subtitleMode == ChannelSubtitleMode.None)
        {
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

    private Option<MediaStream> DefaultSelectAudioStream(
        MediaVersion version,
        IReadOnlyCollection<string> preferredLanguageCodes,
        string preferredAudioTitle)
    {
        var audioStreams = version.Streams.Filter(s => s.MediaStreamKind == MediaStreamKind.Audio).ToList();

        var correctLanguage = audioStreams.Filter(
            s => preferredLanguageCodes.Any(
                c => string.Equals(
                    s.Language,
                    c,
                    StringComparison.InvariantCultureIgnoreCase))).ToList();

        if (correctLanguage.Any())
        {
            _logger.LogDebug(
                "Found {Count} audio streams with preferred audio language code(s) {Code}",
                correctLanguage.Count,
                preferredLanguageCodes);

            return PrioritizeAudioTitle(correctLanguage, preferredAudioTitle ?? string.Empty);
        }

        _logger.LogDebug(
            "Unable to find audio stream with preferred audio language code(s) {Code}",
            preferredLanguageCodes);

        return PrioritizeAudioTitle(audioStreams, preferredAudioTitle ?? string.Empty);
    }

    private Option<MediaStream> PrioritizeAudioTitle(IReadOnlyCollection<MediaStream> streams, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return PrioritizeDefault(streams);
        }

        // prioritize matching titles
        var matchingTitle = streams
            .Filter(ms => (ms.Title ?? string.Empty).Contains(title, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matchingTitle.Any())
        {
            _logger.LogDebug(
                "Found {Count} audio streams with preferred title {Title}",
                matchingTitle.Count,
                title);

            return PrioritizeDefault(streams);
        }

        _logger.LogDebug("Unable to find audio stream with preferred title {Title}", title);

        return PrioritizeDefault(streams);
    }

    private Option<MediaStream> PrioritizeDefault(IReadOnlyCollection<MediaStream> streams)
    {
        var sorted = streams.OrderByDescending(s => s.Channels).ToList();
        Option<MediaStream> maybeDefault = Optional(sorted.Find(s => s.Default));
        foreach (MediaStream stream in maybeDefault)
        {
            _logger.LogDebug("Found audio stream flagged as default");
            return stream;
        }

        _logger.LogDebug("Unable to find default audio stream; selecting stream with most channels");

        return streams.HeadOrNone();
    }

    private async Task<Option<MediaStream>> SelectEpisodeAudioStream(
        Channel channel,
        List<string> preferredLanguageCodes,
        int episodeId,
        MediaVersion version)
    {
        string jsScriptPath = Path.ChangeExtension(
            Path.Combine(FileSystemLayout.AudioStreamSelectorScriptsFolder, "episode"),
            "js");

        _logger.LogDebug("Checking for JS Script at {Path}", jsScriptPath);
        if (!_localFileSystem.FileExists(jsScriptPath))
        {
            _logger.LogDebug("Unable to locate episode audio stream selector script; falling back to built-in logic");
            return Option<MediaStream>.None;
        }

        _logger.LogDebug("Found JS Script at {Path}", jsScriptPath);

        await _scriptEngine.LoadAsync(jsScriptPath);

        EpisodeAudioStreamSelectorData data = await _streamSelectorRepository.GetEpisodeData(episodeId);

        AudioStream[] audioStreams = GetAudioStreamsForScript(version);

        object result = _scriptEngine.Invoke(
            "selectEpisodeAudioStreamIndex",
            channel.Number,
            channel.Name,
            data.ShowTitle,
            data.ShowGuids,
            data.SeasonNumber,
            data.EpisodeNumber,
            data.EpisodeGuids,
            preferredLanguageCodes.ToArray(),
            audioStreams);

        return ProcessScriptResult(version, result);
    }

    private async Task<Option<MediaStream>> SelectMovieAudioStream(
        Channel channel,
        List<string> preferredLanguageCodes,
        int movieId,
        MediaVersion version)
    {
        string jsScriptPath = Path.ChangeExtension(
            Path.Combine(FileSystemLayout.AudioStreamSelectorScriptsFolder, "movie"),
            "js");

        _logger.LogDebug("Checking for JS Script at {Path}", jsScriptPath);
        if (!_localFileSystem.FileExists(jsScriptPath))
        {
            _logger.LogInformation(
                "Unable to locate movie audio stream selector script; falling back to built-in logic");
            return Option<MediaStream>.None;
        }

        _logger.LogDebug("Found JS Script at {Path}", jsScriptPath);

        await _scriptEngine.LoadAsync(jsScriptPath);

        MovieAudioStreamSelectorData data = await _streamSelectorRepository.GetMovieData(movieId);

        AudioStream[] audioStreams = GetAudioStreamsForScript(version);

        object result = _scriptEngine.Invoke(
            "selectMovieAudioStreamIndex",
            channel.Number,
            channel.Name,
            data.Title,
            data.Guids,
            preferredLanguageCodes.ToArray(),
            audioStreams);

        return ProcessScriptResult(version, result);
    }

    private Option<MediaStream> ProcessScriptResult(MediaVersion version, object result)
    {
        if (result is double d)
        {
            var streamIndex = (int)d;
            Option<MediaStream> maybeStream = version.Streams.Find(s => s.Index == streamIndex);
            foreach (MediaStream stream in maybeStream)
            {
                _logger.LogDebug(
                    "JS Script returned audio stream index {Index} with language {Language} and {Channels} audio channel(s)",
                    streamIndex,
                    stream.Language,
                    stream.Channels);
                return stream;
            }

            _logger.LogWarning(
                "JS Script returned audio stream index {Index} which does not exist",
                streamIndex);
        }
        else
        {
            _logger.LogInformation(
                "JS Script did not return an audio stream index; falling back to built-in logic");
        }

        return Option<MediaStream>.None;
    }

    private static AudioStream[] GetAudioStreamsForScript(MediaVersion version) => version.Streams
        .Filter(s => s.MediaStreamKind == MediaStreamKind.Audio)
        .Map(a => new AudioStream(a.Index, a.Channels, a.Codec, a.Default, a.Forced, a.Language, a.Title))
        .ToArray();

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private record AudioStream(
        int index,
        int channels,
        string codec,
        bool isDefault,
        bool isForced,
        string language,
        string title);
}
