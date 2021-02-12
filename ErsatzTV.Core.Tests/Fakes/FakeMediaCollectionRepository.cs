using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.AggregateModels;
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

        public Task<SimpleMediaCollection> Add(SimpleMediaCollection collection) => throw new NotSupportedException();

        public Task<Option<MediaCollection>> Get(int id) => throw new NotSupportedException();

        public Task<Option<SimpleMediaCollection>> GetSimpleMediaCollection(int id) =>
            throw new NotSupportedException();

        public Task<Option<SimpleMediaCollection>> GetSimpleMediaCollectionWithItems(int id) =>
            throw new NotSupportedException();

        public Task<Option<TelevisionMediaCollection>> GetTelevisionMediaCollection(int id) =>
            throw new NotSupportedException();

        public Task<List<SimpleMediaCollection>> GetSimpleMediaCollections() => throw new NotSupportedException();

        public Task<List<MediaCollection>> GetAll() => throw new NotSupportedException();

        public Task<List<MediaCollectionSummary>> GetSummaries(string searchString) =>
            throw new NotSupportedException();

        public Task<Option<List<MediaItem>>> GetItems(int id) => Some(_data[id]).AsTask();

        public Task<Option<List<MediaItem>>> GetSimpleMediaCollectionItems(int id) =>
            throw new NotSupportedException();

        public Task<Option<List<MediaItem>>> GetTelevisionMediaCollectionItems(int id) =>
            throw new NotSupportedException();

        public Task Update(SimpleMediaCollection collection) => throw new NotSupportedException();

        public Task InsertOrIgnore(TelevisionMediaCollection collection) => throw new NotSupportedException();

        public Task<Unit> ReplaceItems(int collectionId, List<MediaItem> mediaItems) =>
            throw new NotSupportedException();

        public Task Delete(int mediaCollectionId) => throw new NotSupportedException();
        public Task DeleteEmptyTelevisionCollections() => throw new NotSupportedException();
    }
}
