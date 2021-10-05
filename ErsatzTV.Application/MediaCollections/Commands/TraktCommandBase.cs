using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Interfaces.Trakt;
using ErsatzTV.Core.Trakt;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public abstract class TraktCommandBase
    {
        private readonly ISearchRepository _searchRepository;
        private readonly ISearchIndex _searchIndex;
        private readonly ILogger _logger;

        protected TraktCommandBase(
            ITraktApiClient traktApiClient,
            ISearchRepository searchRepository,
            ISearchIndex searchIndex,
            ILogger logger)
        {
            _searchRepository = searchRepository;
            _searchIndex = searchIndex;
            _logger = logger;
            TraktApiClient = traktApiClient;
        }

        protected ITraktApiClient TraktApiClient { get; }

        protected static Task<Validation<BaseError, TraktList>>
            TraktListMustExist(TvContext dbContext, int traktListId) =>
            dbContext.TraktLists
                .Include(l => l.Items)
                .ThenInclude(i => i.Guids)
                .SelectOneAsync(c => c.Id, c => c.Id == traktListId)
                .Map(o => o.ToValidation<BaseError>($"TraktList {traktListId} does not exist."));

        protected async Task<Either<BaseError, TraktList>> SaveList(TvContext dbContext, TraktList list)
        {
            Option<TraktList> maybeExisting = await dbContext.TraktLists
                .Include(l => l.Items)
                .ThenInclude(i => i.Guids)
                .SelectOneAsync(tl => tl.Id, tl => tl.User == list.User && tl.List == list.List);

            return await maybeExisting.Match(
                async existing =>
                {
                    existing.Name = list.Name;
                    existing.Description = list.Description;
                    existing.ItemCount = list.ItemCount;

                    await dbContext.SaveChangesAsync();

                    return existing;
                },
                async () =>
                {
                    await dbContext.TraktLists.AddAsync(list);
                    await dbContext.SaveChangesAsync();

                    return list;
                });
        }

        protected async Task<Either<BaseError, TraktList>> SaveListItems(TvContext dbContext, TraktList list)
        {
            Either<BaseError, List<TraktListItemWithGuids>> maybeItems =
                await TraktApiClient.GetUserListItems(list.User, list.List);

            return await maybeItems.Match<Task<Either<BaseError, TraktList>>>(
                async items =>
                {
                    var toAdd = items.Filter(i => list.Items.All(i2 => i2.TraktId != i.TraktId)).ToList();
                    var toRemove = list.Items.Filter(i => items.All(i2 => i2.TraktId != i.TraktId)).ToList();
                    var toUpdate = list.Items.Filter(i => !toRemove.Contains(i)).ToList();
                    
                    list.Items.RemoveAll(toRemove.Contains);
                    list.Items.AddRange(toAdd.Map(a => ProjectItem(list, a)));

                    foreach (TraktListItem existing in toUpdate)
                    {
                        Option<TraktListItem> maybeIncoming = list.Items.Find(i => i.TraktId == existing.TraktId);
                        foreach (TraktListItem incoming in maybeIncoming)
                        {
                            existing.Kind = incoming.Kind;
                            existing.Rank = incoming.Rank;
                            existing.Title = incoming.Title;
                            existing.Year = incoming.Year;
                            existing.Season = incoming.Season;
                            existing.Episode = incoming.Episode;
                            existing.Guids.Clear();
                            existing.Guids.AddRange(incoming.Guids);
                            existing.MediaItemId = null;
                            existing.MediaItem = null;
                        }
                    }

                    await dbContext.SaveChangesAsync();

                    return list;
                },
                error => Task.FromResult(Left<BaseError, TraktList>(error)));
        }

        protected async Task<Either<BaseError, TraktList>> MatchListItems(TvContext dbContext, TraktList list)
        {
            try
            {
                var ids = new System.Collections.Generic.HashSet<int>();

                foreach (TraktListItem item in list.Items
                    .OrderBy(i => i.Title).ThenBy(i => i.Year).ThenBy(i => i.Season).ThenBy(i => i.Episode))
                {
                    switch (item.Kind)
                    {
                        case TraktListItemKind.Movie:
                            Option<int> maybeMovieId = await IdentifyMovie(dbContext, item);
                            foreach (int movieId in maybeMovieId)
                            {
                                ids.Add(movieId);
                                item.MediaItemId = movieId;
                            }

                            break;
                        case TraktListItemKind.Show:
                            Option<int> maybeShowId = await IdentifyShow(dbContext, item);
                            foreach (int showId in maybeShowId)
                            {
                                ids.Add(showId);
                                item.MediaItemId = showId;
                            }

                            break;
                        case TraktListItemKind.Season:
                            Option<int> maybeSeasonId = await IdentifySeason(dbContext, item);
                            foreach (int seasonId in maybeSeasonId)
                            {
                                ids.Add(seasonId);
                                item.MediaItemId = seasonId;
                            }

                            break;
                        default:
                            Option<int> maybeEpisodeId = await IdentifyEpisode(dbContext, item);
                            foreach (int episodeId in maybeEpisodeId)
                            {
                                ids.Add(episodeId);
                                item.MediaItemId = episodeId;
                            }

                            break;
                    }
                }

                await dbContext.SaveChangesAsync();

                foreach (int mediaItemId in ids)
                {
                    Option<MediaItem> maybeItem = await _searchRepository.GetItemToIndex(mediaItemId);
                    foreach (MediaItem item in maybeItem)
                    {
                        await _searchIndex.UpdateItems(_searchRepository, new[] { item }.ToList());
                    }
                }

                _searchIndex.Commit();

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error matching trakt list items");
                return BaseError.New(ex.Message);
            }
        }

        private static TraktListItem ProjectItem(TraktList list, TraktListItemWithGuids item)
        {
            var result = new TraktListItem
            {
                TraktList = list,
                Kind = item.Kind,
                TraktId = item.TraktId,
                Rank = item.Rank,
                Title = item.Title,
                Year = item.Year,
                Season = item.Season,
                Episode = item.Episode,
            };

            result.Guids = item.Guids.Map(g => new TraktListItemGuid { Guid = g, TraktListItem = result }).ToList();

            return result;
        }
        
        private async Task<Option<int>> IdentifyMovie(TvContext dbContext, TraktListItem item)
        {
            var guids = item.Guids.Map(g => g.Guid).ToList();
            
            Option<int> maybeMovieByGuid = await dbContext.MovieMetadata
                .Filter(mm => mm.Guids.Any(g => guids.Contains(g.Guid)))
                .FirstOrDefaultAsync()
                .Map(Optional)
                .MapT(mm => mm.MovieId);

            foreach (int movieId in maybeMovieByGuid)
            {
                _logger.LogDebug("Located trakt movie {Title} by id", item.DisplayTitle);
                return movieId;
            }

            Option<int> maybeMovieByTitleYear = await dbContext.MovieMetadata
                .Filter(mm => mm.Title == item.Title && mm.Year == item.Year)
                .FirstOrDefaultAsync()
                .Map(Optional)
                .MapT(mm => mm.MovieId);

            foreach (int movieId in maybeMovieByTitleYear)
            {
                _logger.LogDebug("Located trakt movie {Title} by title/year", item.DisplayTitle);
                return movieId;
            }

            _logger.LogDebug("Unable to locate trakt movie {Title}", item.DisplayTitle);

            return None;
        }

        private async Task<Option<int>> IdentifyShow(TvContext dbContext, TraktListItem item)
        {
            var guids = item.Guids.Map(g => g.Guid).ToList();

            Option<int> maybeShowByGuid = await dbContext.ShowMetadata
                .Filter(sm => sm.Guids.Any(g => guids.Contains(g.Guid)))
                .FirstOrDefaultAsync()
                .Map(Optional)
                .MapT(sm => sm.ShowId);

            foreach (int showId in maybeShowByGuid)
            {
                _logger.LogDebug("Located trakt show {Title} by id", item.DisplayTitle);
                return showId;
            }

            Option<int> maybeShowByTitleYear = await dbContext.ShowMetadata
                .Filter(sm => sm.Title == item.Title && sm.Year == item.Year)
                .FirstOrDefaultAsync()
                .Map(Optional)
                .MapT(sm => sm.ShowId);

            foreach (int showId in maybeShowByTitleYear)
            {
                _logger.LogDebug("Located trakt show {Title} by title/year", item.Title);
                return showId;
            }

            _logger.LogDebug("Unable to locate trakt show {Title}", item.DisplayTitle);

            return None;
        }
        
        private async Task<Option<int>> IdentifySeason(TvContext dbContext, TraktListItem item)
        {
            var guids = item.Guids.Map(g => g.Guid).ToList();

            Option<int> maybeSeasonByGuid = await dbContext.SeasonMetadata
                .Filter(sm => sm.Guids.Any(g => guids.Contains(g.Guid)))
                .FirstOrDefaultAsync()
                .Map(Optional)
                .MapT(sm => sm.SeasonId);

            foreach (int seasonId in maybeSeasonByGuid)
            {
                _logger.LogDebug("Located trakt season {Title} by id", item.DisplayTitle);
                return seasonId;
            }

            Option<int> maybeSeasonByTitleYear = await dbContext.SeasonMetadata
                .Filter(sm => sm.Season.Show.ShowMetadata.Any(s => s.Title == item.Title && s.Year == item.Year))
                .Filter(sm => sm.Season.SeasonNumber == item.Season)
                .FirstOrDefaultAsync()
                .Map(Optional)
                .MapT(sm => sm.SeasonId);

            foreach (int seasonId in maybeSeasonByTitleYear)
            {
                _logger.LogDebug("Located trakt season {Title} by title/year/season", item.DisplayTitle);
                return seasonId;
            }

            _logger.LogDebug("Unable to locate trakt season {Title}", item.DisplayTitle);

            return None;
        }
        
        private async Task<Option<int>> IdentifyEpisode(TvContext dbContext, TraktListItem item)
        {
            var guids = item.Guids.Map(g => g.Guid).ToList();

            Option<int> maybeEpisodeByGuid = await dbContext.EpisodeMetadata
                .Filter(em => em.Guids.Any(g => guids.Contains(g.Guid)))
                .FirstOrDefaultAsync()
                .Map(Optional)
                .MapT(sm => sm.EpisodeId);

            foreach (int episodeId in maybeEpisodeByGuid)
            {
                _logger.LogDebug("Located trakt episode {Title} by id", item.DisplayTitle);
                return episodeId;
            }

            Option<int> maybeEpisodeByTitleYear = await dbContext.EpisodeMetadata
                .Filter(sm => sm.Episode.Season.Show.ShowMetadata.Any(s => s.Title == item.Title && s.Year == item.Year))
                .Filter(em => em.Episode.Season.SeasonNumber == item.Season)
                .Filter(sm => sm.Episode.EpisodeMetadata.Any(e => e.EpisodeNumber == item.Episode))
                .FirstOrDefaultAsync()
                .Map(Optional)
                .MapT(sm => sm.EpisodeId);

            foreach (int episodeId in maybeEpisodeByTitleYear)
            {
                _logger.LogDebug("Located trakt episode {Title} by title/year/season/episode", item.DisplayTitle);
                return episodeId;
            }

            _logger.LogDebug("Unable to locate trakt episode {Title}", item.DisplayTitle);

            return None;
        }
    }
}
