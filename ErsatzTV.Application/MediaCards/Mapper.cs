﻿using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards
{
    internal static class Mapper
    {
        internal static TelevisionShowCardViewModel ProjectToViewModel(TelevisionShow televisionShow) =>
            new(
                televisionShow.Id,
                televisionShow.Metadata?.Title,
                televisionShow.Metadata?.Year.ToString(),
                televisionShow.Metadata?.SortTitle,
                televisionShow.Poster);

        internal static TelevisionSeasonCardViewModel ProjectToViewModel(TelevisionSeason televisionSeason) =>
            new(
                televisionSeason.TelevisionShow.Metadata?.Title,
                televisionSeason.Id,
                televisionSeason.Number,
                GetSeasonName(televisionSeason.Number),
                string.Empty,
                GetSeasonName(televisionSeason.Number),
                televisionSeason.Poster,
                televisionSeason.Number == 0 ? "S" : televisionSeason.Number.ToString());

        internal static TelevisionEpisodeCardViewModel ProjectToViewModel(
            TelevisionEpisodeMediaItem televisionEpisode) =>
            new(
                televisionEpisode.Id,
                televisionEpisode.Metadata?.Aired ?? DateTime.MinValue,
                televisionEpisode.Season.TelevisionShow.Metadata.Title,
                televisionEpisode.Metadata?.Title,
                $"Episode {televisionEpisode.Metadata?.Episode}",
                televisionEpisode.Metadata?.Episode.ToString(),
                televisionEpisode.Poster,
                televisionEpisode.Metadata?.Episode.ToString());

        internal static MovieCardViewModel ProjectToViewModel(NewMovieMetadata movieMetadata) =>
            new(
                movieMetadata.MovieId,
                movieMetadata.Title,
                movieMetadata.ReleaseDate?.Year.ToString(),
                movieMetadata.SortTitle,
                null); // TODO: artwork

        internal static SimpleMediaCollectionCardResultsViewModel
            ProjectToViewModel(SimpleMediaCollection collection) =>
            new(
                collection.Name,
                // TODO: fix this
                new List<MovieCardViewModel>(), // collection.Movies.Map(ProjectToViewModel).ToList(),
                collection.TelevisionShows.Map(ProjectToViewModel).ToList(),
                collection.TelevisionSeasons.Map(ProjectToViewModel).ToList(),
                collection.TelevisionEpisodes.Map(ProjectToViewModel).ToList());

        private static string GetSeasonName(int number) =>
            number == 0 ? "Specials" : $"Season {number}";
    }
}
