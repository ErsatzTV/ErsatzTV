using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaCollectionRepository
    {
        Task<Option<Collection>> GetCollectionWithCollectionItemsUntracked(int id);
        Task<List<MediaItem>> GetItems(int id);
        Task<List<MediaItem>> GetMultiCollectionItems(int id);
        Task<List<int>> PlayoutIdsUsingCollection(int collectionId);
        Task<List<int>> PlayoutIdsUsingMultiCollection(int multiCollectionId);
        Task<bool> IsCustomPlaybackOrder(int collectionId);
    }
}
