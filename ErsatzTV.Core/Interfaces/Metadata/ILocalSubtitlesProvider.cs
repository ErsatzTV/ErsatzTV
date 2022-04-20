using System.Globalization;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ILocalSubtitlesProvider
{
    List<Subtitle> LocateExternalSubtitles(List<CultureInfo> languageCodes, MediaItem mediaItem);
}
