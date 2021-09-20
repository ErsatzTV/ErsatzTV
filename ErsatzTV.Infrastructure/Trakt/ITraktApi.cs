using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Trakt.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Trakt
{
    [Headers("Accept: application/json", "trakt-api-version: 2")]
    public interface ITraktApi
    {
        [Get("/users/{user}/lists/{list}/items")]
        Task<List<TraktListItem>> GetUserListItems(
            [Header("trakt-api-key")]
            string clientId,
            string user,
            string list);
    }
}
