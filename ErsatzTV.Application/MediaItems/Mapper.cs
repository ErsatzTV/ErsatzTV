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
                mediaItem.LibraryPathId,
                mediaItem.Path);

        internal static MediaItemSearchResultViewModel ProjectToSearchViewModel(MediaItem mediaItem) =>
            mediaItem switch
            {
                TelevisionEpisodeMediaItem e => ProjectToSearchViewModel(e),
                Movie m => ProjectToSearchViewModel(m),
                _ => throw new ArgumentOutOfRangeException()
            };

        private static MediaItemSearchResultViewModel ProjectToSearchViewModel(TelevisionEpisodeMediaItem mediaItem) =>
            new(
                mediaItem.Id,
                GetLibraryName(mediaItem),
                "TV Show",
                GetDisplayTitle(mediaItem),
                GetDisplayDuration(mediaItem));

        private static MediaItemSearchResultViewModel ProjectToSearchViewModel(Movie mediaItem) =>
            new(
                mediaItem.Id,
                GetLibraryName(mediaItem),
                "Movie",
                GetDisplayTitle(mediaItem),
                GetDisplayDuration(mediaItem));


        private static string GetDisplayTitle(MediaItem mediaItem) =>
            mediaItem switch
            {
                TelevisionEpisodeMediaItem e => e.Metadata != null
                    ? $"{e.Metadata.Title} - s{e.Metadata.Season:00}e{e.Metadata.Episode:00}"
                    : Path.GetFileName(e.Path),
                Movie m => m.MovieMetadata.HeadOrNone().Map(mm => mm.Title).IfNone(Path.GetFileName(m.Path)),
                _ => string.Empty
            };

        private static string GetDisplayDuration(MediaItem mediaItem) =>
            string.Format(
                mediaItem.Statistics.Duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
                mediaItem.Statistics.Duration);

        // TODO: fix this
        private static string GetLibraryName(MediaItem item) =>
            "Library Name";

        // source switch
        // {
        //     LocalMediaSource lms => "Local Media Source",
        //     _ => "unknown source"
        // };
    }
}
