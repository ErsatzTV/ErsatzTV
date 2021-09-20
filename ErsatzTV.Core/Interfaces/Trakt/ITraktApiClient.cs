using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Trakt;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Trakt
{
    public interface ITraktApiClient
    {
        Task<Either<BaseError, List<TraktListItemWithGuids>>> GetUserListItems(
            string user,
            string list);
    }
}
