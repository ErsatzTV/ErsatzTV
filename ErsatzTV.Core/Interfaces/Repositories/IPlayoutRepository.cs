using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IPlayoutRepository
    {
        Task<Option<Playout>> GetFull(int id);
        Task<Option<PlayoutItem>> GetPlayoutItem(int channelId, DateTimeOffset now);
        Task<Option<DateTimeOffset>> GetNextItemStart(int channelId, DateTimeOffset now);
        Task<List<PlayoutItem>> GetPlayoutItems(int playoutId);
        Task Update(Playout playout);
    }
}
