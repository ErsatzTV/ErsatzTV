using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Search;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Search
{
    public interface ISearchIndex
    {
        public int Version { get; }
        Task<bool> Initialize();
        Task<Unit> Rebuild(List<MediaItem> items);
        Task<SearchResult> Search(string query, int skip, int limit, string searchField = "");
    }
}
