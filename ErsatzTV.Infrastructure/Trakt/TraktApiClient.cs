using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Trakt;
using ErsatzTV.Core.Trakt;
using ErsatzTV.Infrastructure.Trakt.Models;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ErsatzTV.Infrastructure.Trakt
{
    public class TraktApiClient : ITraktApiClient
    {
        private readonly ITraktApi _traktApi;
        private readonly IOptions<TraktConfiguration> _traktConfiguration;
        private readonly ILogger<TraktApiClient> _logger;

        public TraktApiClient(
            ITraktApi traktApi,
            IOptions<TraktConfiguration> traktConfiguration,
            ILogger<TraktApiClient> logger)
        {
            _traktApi = traktApi;
            _traktConfiguration = traktConfiguration;
            _logger = logger;
        }

        public async Task<Either<BaseError, List<TraktListItemWithGuids>>> GetUserListItems(
            string user,
            string list)
        {
            try
            {
                var result = new List<TraktListItemWithGuids>();
                
                List<TraktListItem> apiItems = await _traktApi.GetUserListItems(
                    _traktConfiguration.Value.ClientId,
                    user,
                    list);

                foreach (TraktListItem apiItem in apiItems)
                {
                    TraktListItemWithGuids item = apiItem.Type.ToLowerInvariant() switch
                    {
                        "movie" => new TraktListItemWithGuids(
                            $"{apiItem.Movie.Title} ({apiItem.Movie.Year})",
                            apiItem.Movie.Title,
                            apiItem.Movie.Year,
                            0,
                            0,
                            TraktListItemKind.Movie,
                            GuidsFromIds(apiItem.Movie.Ids)),
                        "show" => new TraktListItemWithGuids(
                            $"{apiItem.Show.Title} ({apiItem.Show.Year})",
                            apiItem.Show.Title,
                            apiItem.Show.Year,
                            0,
                            0,
                            TraktListItemKind.Show,
                            GuidsFromIds(apiItem.Show.Ids)),
                        "season" => new TraktListItemWithGuids(
                            $"{apiItem.Show.Title} ({apiItem.Show.Year}) S{apiItem.Season.Number:00}",
                            apiItem.Show.Title,
                            apiItem.Show.Year,
                            apiItem.Season.Number,
                            0,
                            TraktListItemKind.Season,
                            GuidsFromIds(apiItem.Season.Ids)),
                        "episode" => new TraktListItemWithGuids(
                            $"{apiItem.Show.Title} ({apiItem.Show.Year}) S{apiItem.Episode.Season:00}E{apiItem.Episode.Number:00}",
                            apiItem.Show.Title,
                            apiItem.Show.Year,
                            apiItem.Episode.Season,
                            apiItem.Episode.Number,
                            TraktListItemKind.Episode,
                            GuidsFromIds(apiItem.Episode.Ids)),
                        _ => null
                    };

                    if (item != null)
                    {
                        result.Add(item);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trakt list items");
                return BaseError.New(ex.Message);
            }
        }

        private static List<string> GuidsFromIds(TraktListItemIds ids)
        {
            var result = new List<string>();

            if (!string.IsNullOrWhiteSpace(ids.Imdb))
            {
                result.Add($"imdb://{ids.Imdb}");
            }

            if (ids.Tmdb.HasValue)
            {
                result.Add($"tmdb://{ids.Tmdb.Value}");
            }

            if (ids.Tvdb.HasValue)
            {
                result.Add($"tvdb://{ids.Tvdb.Value}");
            }

            return result;
        }
    }
}
