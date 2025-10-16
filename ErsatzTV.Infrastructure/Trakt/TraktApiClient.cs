using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Trakt;
using ErsatzTV.Core.Trakt;
using ErsatzTV.Infrastructure.Trakt.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ErsatzTV.Infrastructure.Trakt;

public class TraktApiClient(
    ITraktApi traktApi,
    IOptions<TraktConfiguration> traktConfiguration,
    ILogger<TraktApiClient> logger)
    : ITraktApiClient
{
    public async Task<Either<BaseError, TraktList>> GetUserList(string user, string list)
    {
        try
        {
            TraktListResponse response = string.Equals(user, "official", StringComparison.OrdinalIgnoreCase)
                ? await traktApi.GetOfficialList(traktConfiguration.Value.ClientId, list)
                : await traktApi.GetUserList(traktConfiguration.Value.ClientId, user, list);

            return new TraktList
            {
                TraktId = response.Ids.Trakt,

                // slug must be used here for proper URL generation
                User = response.User.Ids.Slug,

                List = response.Ids.Slug,
                Name = response.Name,
                Description = response.Description,
                ItemCount = response.ItemCount,
                Items = []
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting trakt list");
            return BaseError.New(ex.Message);
        }
    }

    public async Task<Either<BaseError, List<TraktListItemWithGuids>>> GetUserListItems(string user, string list)
    {
        try
        {
            var result = new List<TraktListItemWithGuids>();

            List<TraktListItemResponse> apiItems = string.Equals(user, "official", StringComparison.OrdinalIgnoreCase)
                ? await traktApi.GetOfficialListItems(traktConfiguration.Value.ClientId, list)
                : await traktApi.GetUserListItems(traktConfiguration.Value.ClientId, user, list);

            foreach (TraktListItemResponse apiItem in apiItems)
            {
                TraktListItemWithGuids item = apiItem.Type.ToLowerInvariant() switch
                {
                    "movie" => new TraktListItemWithGuids(
                        apiItem.Id,
                        apiItem.Rank,
                        $"{apiItem.Movie.Title} ({apiItem.Movie.Year})",
                        apiItem.Movie.Title,
                        apiItem.Movie.Year,
                        0,
                        0,
                        TraktListItemKind.Movie,
                        GuidsFromIds(apiItem.Movie.Ids)),
                    "show" => new TraktListItemWithGuids(
                        apiItem.Id,
                        apiItem.Rank,
                        $"{apiItem.Show.Title} ({apiItem.Show.Year})",
                        apiItem.Show.Title,
                        apiItem.Show.Year,
                        0,
                        0,
                        TraktListItemKind.Show,
                        GuidsFromIds(apiItem.Show.Ids)),
                    "season" => new TraktListItemWithGuids(
                        apiItem.Id,
                        apiItem.Rank,
                        $"{apiItem.Show.Title} ({apiItem.Show.Year}) S{apiItem.Season.Number:00}",
                        apiItem.Show.Title,
                        apiItem.Show.Year,
                        apiItem.Season.Number,
                        0,
                        TraktListItemKind.Season,
                        GuidsFromIds(apiItem.Season.Ids)),
                    "episode" => new TraktListItemWithGuids(
                        apiItem.Id,
                        apiItem.Rank,
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
            logger.LogError(ex, "Error getting trakt list items");
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
