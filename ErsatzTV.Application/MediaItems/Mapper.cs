using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaItems
{
    internal static class Mapper
    {
        internal static MediaItemViewModel ProjectToViewModel(MediaItem mediaItem) =>
            new(
                mediaItem.Id,
                mediaItem.MediaSourceId,
                mediaItem.Path);

        internal static MediaItemSearchResultViewModel ProjectToSearchViewModel(MediaItem mediaItem) =>
            new(
                mediaItem.Id,
                GetSourceName(mediaItem.Source),
                mediaItem.Metadata.MediaType.ToString(),
                GetDisplayTitle(mediaItem),
                GetDisplayDuration(mediaItem));


        private static string GetDisplayTitle(this MediaItem mediaItem) =>
            mediaItem.Metadata.MediaType == MediaType.TvShow &&
            Optional(mediaItem.Metadata.SeasonNumber).IsSome &&
            Optional(mediaItem.Metadata.EpisodeNumber).IsSome
                ? $"{mediaItem.Metadata.Title} s{mediaItem.Metadata.SeasonNumber:00}e{mediaItem.Metadata.EpisodeNumber:00}"
                : mediaItem.Metadata.Title;

        private static string GetDisplayDuration(MediaItem mediaItem) =>
            string.Format(
                mediaItem.Metadata.Duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
                mediaItem.Metadata.Duration);

        private static string GetSourceName(MediaSource source) =>
            source switch
            {
                LocalMediaSource lms => lms.Folder,
                _ => source.Name
            };
    }
}
