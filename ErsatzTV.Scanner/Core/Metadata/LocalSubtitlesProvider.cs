using System.Globalization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Metadata;

public class LocalSubtitlesProvider : ILocalSubtitlesProvider
{
    private readonly List<CultureInfo> _languageCodes = new();
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<LocalSubtitlesProvider> _logger;
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly IMetadataRepository _metadataRepository;

    private readonly SemaphoreSlim _slim = new(1, 1);
    private bool _disposedValue;

    public LocalSubtitlesProvider(
        IMediaItemRepository mediaItemRepository,
        IMetadataRepository metadataRepository,
        ILocalFileSystem localFileSystem,
        ILogger<LocalSubtitlesProvider> logger)
    {
        _mediaItemRepository = mediaItemRepository;
        _metadataRepository = metadataRepository;
        _localFileSystem = localFileSystem;
        _logger = logger;
    }

    public async Task<bool> UpdateSubtitles(MediaItem mediaItem, Option<string> localPath, bool saveFullPath)
    {
        if (_languageCodes.Count == 0)
        {
            await _slim.WaitAsync();
            try
            {
                _languageCodes.AddRange(await _mediaItemRepository.GetAllKnownCultures());
            }
            finally
            {
                _slim.Release();
            }
        }

        if (_languageCodes.Count == 0)
        {
            _logger.LogError("Failed to update subtitles; unable to load languages from database");
            return false;
        }

        Option<ErsatzTV.Core.Domain.Metadata> maybeMetadata = mediaItem switch
        {
            Episode e => e.EpisodeMetadata.OfType<ErsatzTV.Core.Domain.Metadata>().HeadOrNone(),
            Movie m => m.MovieMetadata.OfType<ErsatzTV.Core.Domain.Metadata>().HeadOrNone(),
            MusicVideo mv => mv.MusicVideoMetadata.OfType<ErsatzTV.Core.Domain.Metadata>().HeadOrNone(),
            OtherVideo ov => ov.OtherVideoMetadata.OfType<ErsatzTV.Core.Domain.Metadata>().HeadOrNone(),
            _ => None
        };

        if (maybeMetadata.IsNone)
        {
            _logger.LogError(
                "Failed to update subtitles; unable to load metadata for media item type {Type}",
                mediaItem
                    .GetType().Name);
        }

        foreach (ErsatzTV.Core.Domain.Metadata metadata in maybeMetadata)
        {
            MediaVersion version = mediaItem.GetHeadVersion();
            var subtitleStreams = version.Streams
                .Filter(s => s.MediaStreamKind == MediaStreamKind.Subtitle)
                .ToList();

            var subtitles = subtitleStreams.Map(Subtitle.FromMediaStream).ToList();
            string mediaItemPath = await localPath.IfNoneAsync(() => mediaItem.GetHeadVersion().MediaFiles.Head().Path);
            subtitles.AddRange(LocateExternalSubtitles(_languageCodes, mediaItemPath, saveFullPath));
            bool updateResult = await _metadataRepository.UpdateSubtitles(metadata, subtitles);
            if (!updateResult)
            {
                _logger.LogError("Failed to save {Count} subtitles to database", subtitles.Count);
            }

            return updateResult;
        }

        return false;
    }

    public List<Subtitle> LocateExternalSubtitles(
        List<CultureInfo> languageCodes,
        string mediaItemPath,
        bool saveFullPath)
    {
        var result = new List<Subtitle>();

        string? folder = Path.GetDirectoryName(mediaItemPath);
        string withoutExtension = Path.GetFileNameWithoutExtension(mediaItemPath);
        foreach (string file in _localFileSystem.ListFiles(folder, $"{withoutExtension}*"))
        {
            string fileName = Path.GetFileName(file);
            if (!fileName.StartsWith(withoutExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string extension = Path.GetExtension(file);
            string codec = extension switch
            {
                ".ssa" or ".ass" => "ass",
                ".srt" => "subrip",
                ".vtt" => "webvtt",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(codec))
            {
                continue;
            }

            bool forced = file.Contains(".forced.");
            bool sdh = file.Contains(".sdh.") || file.Contains(".cc.");

            string language = fileName
                .Replace($"{withoutExtension}.", string.Empty)[..3]
                .Replace(".", string.Empty);

            Option<CultureInfo> maybeCulture = languageCodes.Find(
                ci => ci.TwoLetterISOLanguageName == language || ci.ThreeLetterISOLanguageName == language);

            foreach (CultureInfo culture in maybeCulture)
            {
                _logger.LogDebug("Located {Attribute} at {Path}", "External Subtitles", file);

                result.Add(
                    new Subtitle
                    {
                        SubtitleKind = SubtitleKind.Sidecar,
                        Codec = codec,
                        Default = false,
                        Forced = forced,
                        SDH = sdh,
                        Language = culture.ThreeLetterISOLanguageName,
                        Path = saveFullPath ? file : Path.GetFileName(file),
                        DateAdded = DateTime.UtcNow,
                        DateUpdated = _localFileSystem.GetLastWriteTime(file)
                    });
            }
        }


        return result;
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
            if (disposing)
            {
                _slim.Dispose();
            }

            _disposedValue = true;
        }
    }
}
