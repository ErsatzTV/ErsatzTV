using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface ISearchRepository
    {
        public Task<List<MediaItem>> SearchMediaItems(string query);
    }
}
