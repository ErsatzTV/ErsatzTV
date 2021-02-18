using System;
using System.IO;
using ErsatzTV.Core.Domain;

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
            mediaItem switch
            {
                TelevisionEpisodeMediaItem e => ProjectToSearchViewModel(e),
                MovieMediaItem m => ProjectToSearchViewModel(m),
                _ => throw new ArgumentOutOfRangeException()
            };

        private static MediaItemSearchResultViewModel ProjectToSearchViewModel(TelevisionEpisodeMediaItem mediaItem) =>
            new(
                mediaItem.Id,
                GetSourceName(mediaItem.Source),
                "TV Show",
                GetDisplayTitle(mediaItem),
                GetDisplayDuration(mediaItem));

        private static MediaItemSearchResultViewModel ProjectToSearchViewModel(MovieMediaItem mediaItem) =>
            new(
                mediaItem.Id,
                GetSourceName(mediaItem.Source),
                "Movie",
                GetDisplayTitle(mediaItem),
                GetDisplayDuration(mediaItem));


        private static string GetDisplayTitle(MediaItem mediaItem) =>
            mediaItem switch
            {
                TelevisionEpisodeMediaItem e => e.Metadata != null
                    ? $"{e.Metadata.Title} - s{e.Metadata.Season:00}e{e.Metadata.Episode:00}"
                    : Path.GetFileName(e.Path),
                MovieMediaItem m => m.Metadata?.Title ?? Path.GetFileName(m.Path),
                _ => string.Empty
            };

        private static string GetDisplayDuration(MediaItem mediaItem) =>
            string.Format(
                mediaItem.Statistics.Duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
                mediaItem.Statistics.Duration);

        private static string GetSourceName(MediaSource source) =>
            source switch
            {
                LocalMediaSource lms => lms.Folder,
                _ => source.Name
            };
    }
}
