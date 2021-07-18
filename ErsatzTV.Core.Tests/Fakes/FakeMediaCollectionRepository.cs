using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling;
using LanguageExt;

namespace ErsatzTV.Core.Tests.Fakes
{
    public class FakeMediaCollectionRepository : IMediaCollectionRepository
    {
        private readonly Map<int, List<MediaItem>> _data;

        public FakeMediaCollectionRepository(Map<int, List<MediaItem>> data) => _data = data;

        public Task<Option<Collection>> GetCollectionWithCollectionItemsUntracked(int id) =>
            throw new NotSupportedException();

        public Task<List<MediaItem>> GetItems(int id) => _data[id].ToList().AsTask();
        public Task<List<MediaItem>> GetMultiCollectionItems(int id) => throw new NotSupportedException();

        public Task<List<CollectionWithItems>> GetMultiCollectionCollections(int id) =>
            throw new NotSupportedException();

        public Task<List<int>> PlayoutIdsUsingCollection(int collectionId) => throw new NotSupportedException();

        public Task<List<int>> PlayoutIdsUsingMultiCollection(int multiCollectionId) =>
            throw new NotSupportedException();

        public Task<bool> IsCustomPlaybackOrder(int collectionId) => false.AsTask();
    }
}
