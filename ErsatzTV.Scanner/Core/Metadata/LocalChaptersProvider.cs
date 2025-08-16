using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Xml;

namespace ErsatzTV.Scanner.Core.Metadata;

public partial class LocalChaptersProvider : ILocalChaptersProvider
{
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<LocalChaptersProvider> _logger;
    private readonly IMetadataRepository _metadataRepository;

    private bool _disposedValue;

    public LocalChaptersProvider(
        IMetadataRepository metadataRepository,
        ILocalFileSystem localFileSystem,
        ILogger<LocalChaptersProvider> logger)
    {
        _metadataRepository = metadataRepository;
        _localFileSystem = localFileSystem;
        _logger = logger;
    }

    public async Task<bool> UpdateChapters(MediaItem mediaItem, Option<string> localPath)
    {
        try
        {
            MediaVersion version = mediaItem.GetHeadVersion();
            string mediaItemPath = await localPath.IfNoneAsync(() => version.MediaFiles.Head().Path);

            List<MediaChapter> chapters = LocateExternalChapters(mediaItemPath);

            if (chapters.Count > 0)
            {
                _logger.LogDebug("Located {Count} external chapters for {Path}", chapters.Count, mediaItemPath);
                return await _metadataRepository.UpdateChapters(version, chapters);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chapters for media item {MediaItemId}", mediaItem.Id);
            return false;
        }
    }

    public List<MediaChapter> LocateExternalChapters(string mediaItemPath)
    {
        var result = new List<MediaChapter>();

        string? folder = Path.GetDirectoryName(mediaItemPath);
        string withoutExtension = Path.GetFileNameWithoutExtension(mediaItemPath);

        foreach (string file in _localFileSystem.ListFiles(folder, $"{withoutExtension}*"))
        {
            string lowerFile = file.ToLowerInvariant();
            string fileName = Path.GetFileName(file);

            if (!fileName.StartsWith(withoutExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string extension = Path.GetExtension(lowerFile);
            if (extension is not (".xml" or ".chapters"))
            {
                continue;
            }

            try
            {
                List<MediaChapter> chapters = ParseChapterFile(file);
                if (chapters.Count > 0)
                {
                    _logger.LogDebug("Located external chapter file at {Path}", file);
                    result.AddRange(chapters);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse chapter file at {Path}", file);
            }
        }

        return result;
    }

    private List<MediaChapter> ParseChapterFile(string filePath)
    {
        var chapters = new List<MediaChapter>();

        try
        {
            var doc = new XmlDocument();
            doc.Load(filePath);

            // Check if this is a Matroska XML chapter file
            XmlNode? chaptersNode = doc.SelectSingleNode("//Chapters") ?? doc.SelectSingleNode("//chapters");
            if (chaptersNode != null)
            {
                return ParseMatroskaXmlChapters(chaptersNode);
            }

            _logger.LogWarning("Unsupported chapter file format: {Path}", filePath);
        }
        catch (XmlException ex)
        {
            _logger.LogWarning(ex, "Invalid XML in chapter file: {Path}", filePath);
        }

        return chapters;
    }

    private static List<MediaChapter> ParseMatroskaXmlChapters(XmlNode chaptersNode)
    {
        var chapters = new List<MediaChapter>();

        XmlNodeList? chapterAtoms = chaptersNode.SelectNodes(".//ChapterAtom") ??
                                    chaptersNode.SelectNodes(".//chapteratom");

        if (chapterAtoms == null)
        {
            return chapters;
        }

        long chapterId = 0;
        foreach (XmlNode chapterAtom in chapterAtoms)
        {
            var chapter = ParseChapterAtom(chapterAtom, chapterId++);
            if (chapter != null)
            {
                chapters.Add(chapter);
            }
        }

        chapters.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

        for (int i = 0; i < chapters.Count; i++)
        {
            chapters[i].ChapterId = i;
        }

        return chapters;
    }

    private static MediaChapter? ParseChapterAtom(XmlNode chapterAtom, long chapterId)
    {
        XmlNode? startNode = chapterAtom.SelectSingleNode(".//ChapterTimeStart") ??
                             chapterAtom.SelectSingleNode(".//chaptertimestart");

        if (startNode?.InnerText == null)
        {
            return null;
        }

        if (!TryParseMatroskaTime(startNode.InnerText, out TimeSpan startTime))
        {
            return null;
        }

        TimeSpan endTime = TimeSpan.Zero;
        XmlNode? endNode = chapterAtom.SelectSingleNode(".//ChapterTimeEnd") ??
                           chapterAtom.SelectSingleNode(".//chaptertimeend");

        if (endNode?.InnerText != null)
        {
            _ = TryParseMatroskaTime(endNode.InnerText, out endTime);
        }

        string title = string.Empty;
        XmlNode? titleNode = chapterAtom.SelectSingleNode(".//ChapterString") ??
                             chapterAtom.SelectSingleNode(".//ChapString") ??
                             chapterAtom.SelectSingleNode(".//chapterstring") ??
                             chapterAtom.SelectSingleNode(".//chapstring");

        if (titleNode?.InnerText != null)
        {
            title = titleNode.InnerText.Trim();
        }

        return new MediaChapter
        {
            ChapterId = chapterId,
            StartTime = startTime,
            EndTime = endTime,
            Title = title
        };
    }

    private static bool TryParseMatroskaTime(string timeString, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;

        if (string.IsNullOrWhiteSpace(timeString))
        {
            return false;
        }

        // Handle nanoseconds format (raw timestamp)
        if (long.TryParse(timeString, out long nanoseconds))
        {
            timeSpan = TimeSpan.FromTicks(nanoseconds / 100);
            return true;
        }

        // Handle time format HH:MM:SS.mmm or HH:MM:SS,mmm
        var timeFormats = new Regex[]
        {
            GetParseFullTimeCodeRegex(),
            GetParseTimeCodeNoMilliRegex()
        };

        foreach (Regex pattern in timeFormats)
        {
            var match = pattern.Match(timeString);
            if (match.Success)
            {
                if (int.TryParse(match.Groups[1].Value, out int hours) &&
                    int.TryParse(match.Groups[2].Value, out int minutes) &&
                    int.TryParse(match.Groups[3].Value, out int seconds))
                {
                    int milliseconds = 0;
                    if (match.Groups.Count > 4 && !int.TryParse(match.Groups[4].Value, out milliseconds))
                    {
                        milliseconds = 0;
                    }

                    timeSpan = new TimeSpan(0, hours, minutes, seconds, milliseconds);
                    return true;
                }
            }
        }

        return false;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            _disposedValue = true;
        }
    }

    [GeneratedRegex(@"^(\d{1,2}):(\d{2}):(\d{2})[\.,](\d{3})$")]
    private static partial Regex GetParseFullTimeCodeRegex();

    [GeneratedRegex(@"^(\d{1,2}):(\d{2}):(\d{2})$")]
    private static partial Regex GetParseTimeCodeNoMilliRegex();
}
