using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Trakt.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Trakt
{
    [Headers("Accept: application/json", "trakt-api-version: 2")]
    public interface ITraktApi
    {
        [Get("/users/{user}/lists/{list}")]
        Task<TraktListResponse> GetUserList(
            [Header("trakt-api-key")]
            string clientId,
            string user,
            string list);
        
        [Get("/users/{user}/lists/{list}/items")]
        Task<List<TraktListItemResponse>> GetUserListItems(
            [Header("trakt-api-key")]
            string clientId,
            string user,
            string list);
    }
}
