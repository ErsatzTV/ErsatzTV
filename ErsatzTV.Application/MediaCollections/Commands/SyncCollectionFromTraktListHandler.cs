using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Trakt;
using ErsatzTV.Core.Trakt;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        SyncCollectionFromTraktListHandler : IRequestHandler<SyncCollectionFromTraktList, Either<BaseError, Unit>>
    {
        private readonly ITraktApiClient _traktApiClient;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly ILogger<SyncCollectionFromTraktListHandler> _logger;

        public SyncCollectionFromTraktListHandler(
            ITraktApiClient traktApiClient,
            IDbContextFactory<TvContext> dbContextFactory,
            IMediaCollectionRepository mediaCollectionRepository,
            ChannelWriter<IBackgroundServiceRequest> channel,
            ILogger<SyncCollectionFromTraktListHandler> logger)
        {
            _traktApiClient = traktApiClient;
            _dbContextFactory = dbContextFactory;
            _mediaCollectionRepository = mediaCollectionRepository;
            _channel = channel;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> Handle(
            SyncCollectionFromTraktList request,
            CancellationToken cancellationToken)
        {
            // validate and parse user/list from URL
            const string PATTERN = @"users\/([\w\-_]+)\/lists\/([\w\-_]+)";
            Match match = Regex.Match(request.TraktListUrl, PATTERN);
            if (match.Success)
            {
                string user = match.Groups[1].Value;
                string list = match.Groups[2].Value;

                Either<BaseError, List<TraktListItemWithGuids>> maybeItems =
                    await _traktApiClient.GetUserListItems(user, list);
                return await maybeItems.Match(
                    async items => await SyncCollectionFromItems(request.CollectionId, items),
                    error => Task.FromResult(Left<BaseError, Unit>(error)));
            }

            return BaseError.New("Invalid Trakt List URL");
        }

        private async Task<Either<BaseError, Unit>> SyncCollectionFromItems(int collectionId, List<TraktListItemWithGuids> items)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            Option<Collection> maybeCollection = await dbContext.Collections
                .Include(c => c.MediaItems)
                .SelectOneAsync(c => c.Id, c => c.Id == collectionId);

            foreach (Collection collection in maybeCollection)
            {
                var movieIds = new System.Collections.Generic.HashSet<int>();
                foreach (TraktListItemWithGuids item in items.Filter(i => i.Kind == TraktListItemKind.Movie))
                {
                    foreach (int movieId in await IdentifyMovie(dbContext, item))
                    {
                        movieIds.Add(movieId);
                    }
                }
                
                var showIds = new System.Collections.Generic.HashSet<int>();
                foreach (TraktListItemWithGuids item in items.Filter(i => i.Kind == TraktListItemKind.Show))
                {
                    foreach (int showId in await IdentifyShow(dbContext, item))
                    {
                        showIds.Add(showId);
                    }
                }
                
                var seasonIds = new System.Collections.Generic.HashSet<int>();
                foreach (TraktListItemWithGuids item in items.Filter(i => i.Kind == TraktListItemKind.Season))
                {
                    foreach (int seasonId in await IdentifySeason(dbContext, item))
                    {
                        seasonIds.Add(seasonId);
                    }
                }
                
                var episodeIds = new System.Collections.Generic.HashSet<int>();
                foreach (TraktListItemWithGuids item in items.Filter(i => i.Kind == TraktListItemKind.Episode))
                {
                    foreach (int episodeId in await IdentifyEpisode(dbContext, item))
                    {
                        episodeIds.Add(episodeId);
                    }
                }

                var allIds = movieIds
                    .Append(showIds)
                    .Append(seasonIds)
                    .Append(episodeIds)
                    .ToList();

                collection.MediaItems.RemoveAll(mi => !allIds.Contains(mi.Id));

                List<MediaItem> toAdd = await dbContext.MediaItems
                    .Filter(mi => allIds.Contains(mi.Id))
                    .ToListAsync();

                collection.MediaItems.AddRange(toAdd);

                if (await dbContext.SaveChangesAsync() > 0)
                {
                    // rebuild all playouts that use this collection
                    foreach (int playoutId in await _mediaCollectionRepository.PlayoutIdsUsingCollection(collectionId))
                    {
                        await _channel.WriteAsync(new BuildPlayout(playoutId, true));
                    }
                }
            }

            return Unit.Default;
        }

        private async Task<Option<int>> IdentifyMovie(TvContext dbContext, TraktListItemWithGuids item)
        {
            Option<int> maybeMovieByGuid = await dbContext.MovieMetadata
                .Filter(mm => mm.Guids.Any(g => item.Guids.Contains(g.Guid)))
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

        private async Task<Option<int>> IdentifyShow(TvContext dbContext, TraktListItemWithGuids item)
        {
            Option<int> maybeShowByGuid = await dbContext.ShowMetadata
                .Filter(sm => sm.Guids.Any(g => item.Guids.Contains(g.Guid)))
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
        
        private async Task<Option<int>> IdentifySeason(TvContext dbContext, TraktListItemWithGuids item)
        {
            Option<int> maybeSeasonByGuid = await dbContext.SeasonMetadata
                .Filter(sm => sm.Guids.Any(g => item.Guids.Contains(g.Guid)))
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
        
        private async Task<Option<int>> IdentifyEpisode(TvContext dbContext, TraktListItemWithGuids item)
        {
            Option<int> maybeEpisodeByGuid = await dbContext.EpisodeMetadata
                .Filter(sm => sm.Guids.Any(g => item.Guids.Contains(g.Guid)))
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
