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
                Episode e => ProjectToSearchViewModel(e),
                Movie m => ProjectToSearchViewModel(m),
                _ => throw new ArgumentOutOfRangeException()
            };

        private static MediaItemSearchResultViewModel ProjectToSearchViewModel(Episode mediaItem) =>
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
                Episode e => e.EpisodeMetadata.HeadOrNone()
                    .Map(em => $"{em.Title} - s{e.Season.SeasonNumber:00}e{e.EpisodeNumber:00}")
                    .IfNone(Path.GetFileName(e.Path)),
                Movie m => m.MovieMetadata.HeadOrNone().Map(mm => mm.Title).IfNone(Path.GetFileName(m.Path)),
                _ => string.Empty
            };

        private static string GetDisplayDuration(MediaItem mediaItem) =>
            string.Format(
                mediaItem.Statistics.Duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
                mediaItem.Statistics.Duration);

        // TODO: fix this when search is reimplemented
        private static string GetLibraryName(MediaItem item) =>
            "Library Name";

        // source switch
        // {
        //     LocalMediaSource lms => "Local Media Source",
        //     _ => "unknown source"
        // };

        public static NamedMediaItemViewModel ProjectToViewModel(Show show) =>
            new(show.Id, show.ShowMetadata.HeadOrNone().Map(sm => $"{sm?.Title} ({sm?.Year})").IfNone("???"));

        public static NamedMediaItemViewModel ProjectToViewModel(Season season) =>
            new(season.Id, $"{ShowTitle(season)} ({SeasonDescription(season)})");

        private static string ShowTitle(Season season) =>
            season.Show.ShowMetadata.HeadOrNone().Map(sm => sm.Title).IfNone("???");

        private static string SeasonDescription(Season season) =>
            season.SeasonNumber == 0 ? "Specials" : $"Season {season.SeasonNumber}";
    }
}
