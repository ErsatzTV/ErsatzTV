using System.Globalization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;

namespace ErsatzTV.Core.Metadata;

public class LocalSubtitlesProvider : ILocalSubtitlesProvider
{
    private readonly ILocalFileSystem _localFileSystem;

    public LocalSubtitlesProvider(ILocalFileSystem localFileSystem)
    {
        _localFileSystem = localFileSystem;
    }

    public List<Subtitle> LocateExternalSubtitles(List<CultureInfo> languageCodes, MediaItem mediaItem)
    {
        var result = new List<Subtitle>();
        
        string mediaItemPath = mediaItem.GetHeadVersion().MediaFiles.Head().Path;
        string folder = Path.GetDirectoryName(mediaItemPath);
        string withoutExtension = Path.GetFileNameWithoutExtension(mediaItemPath);
        foreach (string file in _localFileSystem.ListFiles(folder))
        {
            string fileName = Path.GetFileName(file);
            if (!fileName.StartsWith(withoutExtension))
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
                result.Add(
                    new Subtitle
                    {
                        SubtitleKind = SubtitleKind.Sidecar,
                        Codec = codec,
                        Default = false,
                        Forced = forced,
                        SDH = sdh,
                        Language = culture.ThreeLetterISOLanguageName,
                        Path = file,
                        DateAdded = DateTime.UtcNow,
                        DateUpdated = _localFileSystem.GetLastWriteTime(file)
                    });
            }
        }


        return result;
    }
}
