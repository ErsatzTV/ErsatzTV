using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IPlayoutRepository
    {
        Task<Playout> Add(Playout playout);
        Task<Option<Playout>> Get(int id);
        Task<Option<Playout>> GetFull(int id);
        Task<Option<PlayoutItem>> GetPlayoutItem(int channelId, DateTimeOffset now);
        Task<List<PlayoutItem>> GetPlayoutItems(int playoutId);
        Task<List<int>> GetPlayoutIdsForMediaItems(Seq<MediaItem> mediaItems);
        Task<List<Playout>> GetAll();
        Task Update(Playout playout);
        Task Delete(int playoutId);
    }
}
