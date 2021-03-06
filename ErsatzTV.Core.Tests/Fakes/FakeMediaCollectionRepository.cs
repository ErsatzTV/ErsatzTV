using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Tests.Fakes
{
    public class FakeMediaCollectionRepository : IMediaCollectionRepository
    {
        private readonly Map<int, List<MediaItem>> _data;

        public FakeMediaCollectionRepository(Map<int, List<MediaItem>> data) => _data = data;
        public Task<Collection> Add(Collection collection) => throw new NotSupportedException();
        public Task<bool> AddMediaItem(int collectionId, int mediaItemId) => throw new NotSupportedException();
        public Task<Option<Collection>> Get(int id) => throw new NotSupportedException();
        public Task<Option<Collection>> GetCollectionWithItems(int id) => throw new NotSupportedException();
        public Task<Option<Collection>> GetCollectionWithItemsUntracked(int id) => throw new NotSupportedException();
        public Task<List<Collection>> GetAll() => throw new NotSupportedException();
        public Task<Option<List<MediaItem>>> GetItems(int id) => Some(_data[id].ToList()).AsTask();
        Task<bool> IMediaCollectionRepository.Update(Collection collection) => throw new NotSupportedException();
        public Task Delete(int collectionId) => throw new NotSupportedException();
        public Task<List<int>> PlayoutIdsUsingCollection(int collectionId) => throw new NotSupportedException();
    }
}
