using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaItemRepository
    {
        public Task<int> Add(MediaItem mediaItem);
        public Task<Option<MediaItem>> Get(int id);
        public Task<List<MediaItem>> GetAll();
        public Task<List<MediaItem>> GetAll(MediaType mediaType);
        public Task<List<MediaItem>> GetAllByMediaSourceId(int mediaSourceId);
        public Task Update(MediaItem mediaItem);
        public Task Delete(int mediaItemId);
    }
}
