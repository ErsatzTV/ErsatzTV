using System.IO.Enumeration;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg.Selector;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ErsatzTV.Core.FFmpeg;

public class CustomStreamSelector(ILocalFileSystem localFileSystem, ILogger<CustomStreamSelector> logger) : ICustomStreamSelector
{
    public async Task<StreamSelectorResult> SelectStreams(Channel channel, MediaItemAudioVersion audioVersion, List<Subtitle> allSubtitles)
    {
        try
        {
            string streamSelectorFile = Path.Combine(
                FileSystemLayout.ChannelStreamSelectorsFolder,
                channel.StreamSelector);

            if (!localFileSystem.FileExists(streamSelectorFile))
            {
                logger.LogWarning("YAML stream selector file {File} does not exist; aborting.", channel.StreamSelector);
                return StreamSelectorResult.None;
            }

            StreamSelector streamSelector = await LoadStreamSelector(streamSelectorFile);

            var audioStreams = audioVersion.MediaVersion.Streams
                .Where(s => s.MediaStreamKind == MediaStreamKind.Audio)
                .ToList();

            foreach (StreamSelectorItem streamSelectorItem in streamSelector.Items)
            {
                var candidateAudioStreams = audioStreams.ToList();
                var candidateSubtitles = allSubtitles.ToList();

                // try to find matching audio stream
                foreach (MediaStream audioStream in audioStreams.ToList())
                {
                    var matches = false;
                    string safeTitle = audioStream.Title ?? string.Empty;

                    if (streamSelectorItem.AudioLanguages.Count > 0)
                    {
                        // match any of the listed languages
                        foreach (string audioLanguage in streamSelectorItem.AudioLanguages)
                        {
                            // special case
                            if (audioLanguage == "*")
                            {
                                matches = true;
                            }

                            matches = matches || FileSystemName.MatchesSimpleExpression(
                                audioLanguage.ToLowerInvariant(),
                                audioStream.Language.ToLowerInvariant());
                        }
                    }
                    else
                    {
                        matches = true;
                    }

                    if (streamSelectorItem.AudioTitleBlocklist
                        .Any(block => safeTitle.Contains(block, StringComparison.OrdinalIgnoreCase)))
                    {
                        matches = false;
                    }

                    if (streamSelectorItem.AudioTitleAllowlist.Count > 0)
                    {
                        int matchCount = streamSelectorItem.AudioTitleAllowlist
                            .Count(block => safeTitle.Contains(block, StringComparison.OrdinalIgnoreCase));

                        if (matchCount == 0)
                        {
                            matches = false;
                        }
                    }

                    if (!matches)
                    {
                        candidateAudioStreams.Remove(audioStream);

                        logger.LogDebug(
                            "Audio stream {@Stream} does not match selector item {@SelectorItem}",
                            new { audioStream.Language, audioStream.Title },
                            streamSelectorItem);
                    }
                    else
                    {
                        logger.LogDebug(
                            "Audio stream {@Stream} matches selector item {@SelectorItem}",
                            new { audioStream.Language, audioStream.Title },
                            streamSelectorItem);
                    }
                }

                // try to find matching subtitle stream
                if (streamSelectorItem.DisableSubtitles)
                {
                    candidateSubtitles.Clear();
                }
                else
                {
                    foreach (Subtitle subtitle in allSubtitles.ToList())
                    {
                        var matches = false;
                        string safeTitle = subtitle.Title ?? string.Empty;

                        if (streamSelectorItem.SubtitleLanguages.Count > 0)
                        {
                            // match any of the listed languages
                            foreach (string subtitleLanguage in streamSelectorItem.SubtitleLanguages)
                            {
                                // special case
                                if (subtitleLanguage == "*")
                                {
                                    matches = true;
                                }

                                matches = matches || FileSystemName.MatchesSimpleExpression(
                                    subtitleLanguage,
                                    subtitle.Language);
                            }
                        }

                        if (streamSelectorItem.SubtitleTitleBlocklist
                            .Any(block => safeTitle.Contains(block, StringComparison.OrdinalIgnoreCase)))
                        {
                            matches = false;
                        }

                        if (streamSelectorItem.SubtitleTitleAllowlist.Count > 0)
                        {
                            int matchCount = streamSelectorItem.SubtitleTitleAllowlist
                                .Count(block => safeTitle.Contains(block, StringComparison.OrdinalIgnoreCase));

                            if (matchCount == 0)
                            {
                                matches = false;
                            }
                        }

                        if (!matches)
                        {
                            candidateSubtitles.Remove(subtitle);

                            logger.LogDebug(
                                "Subtitle {@Subtitle} does not match selector item {@SelectorItem}",
                                new { subtitle.Language, subtitle.Title },
                                streamSelectorItem);
                        }
                        else
                        {
                            logger.LogDebug(
                                "Subtitle {@Subtitle} matches selector item {@SelectorItem}",
                                new { subtitle.Language, subtitle.Title },
                                streamSelectorItem);
                        }
                    }
                }

                Option<MediaStream> maybeAudioStream = candidateAudioStreams.HeadOrNone();
                Option<Subtitle> maybeSubtitle = candidateSubtitles.HeadOrNone();

                if (maybeAudioStream.IsSome)
                {
                    return new StreamSelectorResult(maybeAudioStream, maybeSubtitle);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error selecting streams");
        }

        return StreamSelectorResult.None;
    }

    private async Task<StreamSelector> LoadStreamSelector(string streamSelectorFile)
    {
        try
        {
            string yaml = await localFileSystem.ReadAllText(streamSelectorFile);

            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<StreamSelector>(yaml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error loading YAML stream selector");
            throw;
        }
    }
}
