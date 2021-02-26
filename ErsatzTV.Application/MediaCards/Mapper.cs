using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards
{
    internal static class Mapper
    {
        internal static TelevisionShowCardViewModel ProjectToViewModel(ShowMetadata showMetadata) =>
            new(
                showMetadata.ShowId,
                showMetadata.Title,
                showMetadata.ReleaseDate?.Year.ToString(),
                showMetadata.SortTitle,
                null); // TODO: artwork

        internal static TelevisionSeasonCardViewModel ProjectToViewModel(Season season) =>
            new(
                season.Show.ShowMetadata.HeadOrNone().Map(m => m.Title).IfNone(string.Empty),
                season.Id,
                season.SeasonNumber,
                GetSeasonName(season.SeasonNumber),
                string.Empty,
                GetSeasonName(season.SeasonNumber),
                season.Poster,
                season.SeasonNumber == 0 ? "S" : season.SeasonNumber.ToString());

        internal static TelevisionEpisodeCardViewModel ProjectToViewModel(
            EpisodeMetadata episodeMetadata) =>
            new(
                episodeMetadata.EpisodeId,
                episodeMetadata.ReleaseDate ?? DateTime.MinValue,
                episodeMetadata.Episode.Season.Show.ShowMetadata.HeadOrNone().Map(m => m.Title).IfNone(string.Empty),
                episodeMetadata.Title,
                $"Episode {episodeMetadata.Episode.EpisodeNumber}",
                episodeMetadata.Episode.EpisodeNumber.ToString(),
                null, // TODO: artwork
                episodeMetadata.Episode.EpisodeNumber.ToString());

        internal static MovieCardViewModel ProjectToViewModel(MovieMetadata movieMetadata) =>
            new(
                movieMetadata.MovieId,
                movieMetadata.Title,
                movieMetadata.ReleaseDate?.Year.ToString(),
                movieMetadata.SortTitle,
                null); // TODO: artwork

        internal static CollectionCardResultsViewModel
            ProjectToViewModel(Collection collection) =>
            new(
                collection.Name,
                // TODO: fix this
                new List<MovieCardViewModel>(), // collection.Movies.Map(ProjectToViewModel).ToList(),
                new List<TelevisionShowCardViewModel>(), // collection.TelevisionShows.Map(ProjectToViewModel).ToList(),
                new List<TelevisionSeasonCardViewModel>(), // collection.TelevisionSeasons.Map(ProjectToViewModel).ToList(),
                new List<TelevisionEpisodeCardViewModel>()); // collection.TelevisionEpisodes.Map(ProjectToViewModel).ToList());

        private static string GetSeasonName(int number) =>
            number == 0 ? "Specials" : $"Season {number}";
    }
}
