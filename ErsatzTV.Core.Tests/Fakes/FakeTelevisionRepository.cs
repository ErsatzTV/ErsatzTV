﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Tests.Fakes;

public class FakeTelevisionRepository : ITelevisionRepository
{
    public Task<bool> AllShowsExist(List<int> showIds) => throw new NotSupportedException();
    public Task<bool> AllSeasonsExist(List<int> seasonIds) => throw new NotSupportedException();

    public Task<bool> AllEpisodesExist(List<int> episodeIds) => throw new NotSupportedException();

    public Task<List<Show>> GetAllShows() => throw new NotSupportedException();

    public Task<Option<Show>> GetShow(int showId) => throw new NotSupportedException();

    public Task<List<ShowMetadata>> GetShowsForCards(List<int> ids) => throw new NotSupportedException();
    public Task<List<SeasonMetadata>> GetSeasonsForCards(List<int> ids) => throw new NotSupportedException();

    public Task<List<EpisodeMetadata>> GetEpisodesForCards(List<int> ids) => throw new NotSupportedException();

    public Task<List<Episode>> GetShowItems(int showId) => throw new NotSupportedException();

    public Task<List<Season>> GetAllSeasons() => throw new NotSupportedException();

    public Task<Option<Season>> GetSeason(int seasonId) => throw new NotSupportedException();

    public Task<int> GetSeasonCount(int showId) => throw new NotSupportedException();

    public Task<List<Season>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize) =>
        throw new NotSupportedException();

    public Task<List<Episode>> GetSeasonItems(int seasonId) => throw new NotSupportedException();

    public Task<int> GetEpisodeCount(int seasonId) => throw new NotSupportedException();

    public Task<List<EpisodeMetadata>> GetPagedEpisodes(int seasonId, int pageNumber, int pageSize) =>
        throw new NotSupportedException();

    public Task<Option<Show>> GetShowByMetadata(int libraryPathId, ShowMetadata metadata, string showName) =>
        throw new NotSupportedException();

    public Task<Either<BaseError, MediaItemScanResult<Show>>> AddShow(int libraryPathId, ShowMetadata metadata) =>
        throw new NotSupportedException();

    public Task<Either<BaseError, Season>> GetOrAddSeason(Show show, int libraryPathId, int seasonNumber) =>
        throw new NotSupportedException();

    public Task<Either<BaseError, Episode>> GetOrAddEpisode(
        Season season,
        LibraryPath libraryPath,
        LibraryFolder libraryFolder,
        string path) =>
        throw new NotSupportedException();

    public Task<IEnumerable<string>> FindEpisodePaths(LibraryPath libraryPath) => throw new NotSupportedException();

    public Task<Unit> DeleteByPath(LibraryPath libraryPath, string path) => throw new NotSupportedException();

    public Task<Unit> DeleteEmptySeasons(LibraryPath libraryPath) => throw new NotSupportedException();

    public Task<List<int>> DeleteEmptyShows(LibraryPath libraryPath) => throw new NotSupportedException();

    public Task<bool> AddGenre(ShowMetadata metadata, Genre genre) => throw new NotSupportedException();
    public Task<bool> AddGenre(EpisodeMetadata metadata, Genre genre) => throw new NotSupportedException();

    public Task<bool> AddTag(ErsatzTV.Core.Domain.Metadata metadata, Tag tag) => throw new NotSupportedException();

    public Task<bool> AddStudio(ShowMetadata metadata, Studio studio) => throw new NotSupportedException();
    public Task<bool> AddActor(ShowMetadata metadata, Actor actor) => throw new NotSupportedException();

    public Task<bool> AddActor(EpisodeMetadata metadata, Actor actor) => throw new NotSupportedException();

    public Task<Unit> RemoveMetadata(Episode episode, EpisodeMetadata metadata) =>
        throw new NotSupportedException();

    public Task<bool> AddDirector(EpisodeMetadata metadata, Director director) => throw new NotSupportedException();

    public Task<bool> AddWriter(EpisodeMetadata metadata, Writer writer) => throw new NotSupportedException();

    public Task<bool> UpdateTitles(EpisodeMetadata metadata, string title, string sortTitle) =>
        throw new NotSupportedException();

    public Task<bool> UpdateOutline(EpisodeMetadata metadata, string outline) => throw new NotSupportedException();

    public Task<bool> UpdatePlot(EpisodeMetadata metadata, string plot) => throw new NotSupportedException();
    public Task<bool> UpdateYear(ShowMetadata metadata, int? year) => throw new NotSupportedException();

    public Task<bool> UpdateTitles(ShowMetadata metadata, string title, string sortTitle) =>
        throw new NotSupportedException();
}
