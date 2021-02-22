using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaSourceRepository
    {
        Task<LocalMediaSource> Add(LocalMediaSource localMediaSource);
        Task<PlexMediaSource> Add(PlexMediaSource plexMediaSource);
        Task<List<MediaSource>> GetAll();
        Task<List<PlexMediaSource>> GetAllPlex();
        Task<Option<MediaSource>> Get(int id);
        Task<Option<PlexMediaSource>> GetPlex(int id);
        Task<int> CountMediaItems(int id);
        Task Update(LocalMediaSource localMediaSource);
        Task Update(PlexMediaSource plexMediaSource);
        Task Delete(int id);
    }
}
