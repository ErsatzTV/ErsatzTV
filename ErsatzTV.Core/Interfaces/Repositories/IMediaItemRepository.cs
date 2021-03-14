using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaItemRepository
    {
        Task<Option<MediaItem>> Get(int id);
        Task<List<MediaItem>> GetAll();
        Task<List<MediaItem>> Search(string searchString);
        Task<bool> Update(MediaItem mediaItem);
    }
}
