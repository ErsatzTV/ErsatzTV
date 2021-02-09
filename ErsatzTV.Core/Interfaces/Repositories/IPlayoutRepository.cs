using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IPlayoutRepository
    {
        public Task<Playout> Add(Playout playout);
        public Task<Option<Playout>> Get(int id);
        public Task<Option<Playout>> GetFull(int id);
        public Task<Option<PlayoutItem>> GetPlayoutItem(int channelId, DateTimeOffset now);
        public Task<List<PlayoutItem>> GetPlayoutItems(int playoutId);
        public Task<List<int>> GetPlayoutIdsForMediaItems(Seq<MediaItem> mediaItems);
        public Task<List<Playout>> GetAll();
        public Task Update(Playout playout);
        public Task Delete(int playoutId);
    }
}
