using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Models.UI
{
    public static class MediaItemExtensions
    {
        public static string GetDisplayDuration(this MediaItem mediaItem) =>
            string.Format(
                mediaItem.Metadata.Duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
                mediaItem.Metadata.Duration);

        public static string GetDisplayTitle(this MediaItem mediaItem) =>
            mediaItem.Metadata.MediaType == MediaType.TvShow &&
            Optional(mediaItem.Metadata.SeasonNumber).IsSome &&
            Optional(mediaItem.Metadata.EpisodeNumber).IsSome
                ? $"{mediaItem.Metadata.Title} s{mediaItem.Metadata.SeasonNumber:00}e{mediaItem.Metadata.EpisodeNumber:00}"
                : mediaItem.Metadata.Title;

        public static string GetDisplayMediaType(this MediaItem mediaItem) =>
            mediaItem.Metadata.MediaType == MediaType.TvShow
                ? "TV Show"
                : mediaItem.Metadata.MediaType.ToString();
    }
}
