using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaSourceRepository
    {
        public Task<LocalMediaSource> Add(LocalMediaSource localMediaSource);
        public Task<PlexMediaSource> Add(PlexMediaSource plexMediaSource);
        public Task<List<MediaSource>> GetAll();
        public Task<List<PlexMediaSource>> GetAllPlex();
        public Task<Option<MediaSource>> Get(int id);
        public Task<Option<PlexMediaSource>> GetPlex(int id);
        public Task<int> CountMediaItems(int id);
        public Task Update(PlexMediaSource plexMediaSource);
        public Task Delete(int id);
    }
}
