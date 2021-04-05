using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Search;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Search
{
    public interface ISearchIndex : IDisposable
    {
        public int Version { get; }
        Task<bool> Initialize(ILocalFileSystem localFileSystem);
        Task<Unit> Rebuild(ISearchRepository searchRepository, List<int> itemIds);
        Task<Unit> AddItems(List<MediaItem> items);
        Task<Unit> UpdateItems(List<MediaItem> items);
        Task<Unit> RemoveItems(List<int> ids);
        Task<SearchResult> Search(string query, int skip, int limit, string searchField = "");
        void Commit();
    }
}
