using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IChannelRepository
    {
        Task<Channel> Add(Channel channel);
        Task<Option<Channel>> Get(int id);
        Task<Option<Channel>> GetByNumber(string number);
        Task<List<Channel>> GetAll();
        Task<List<Channel>> GetAllForGuide();
        Task<bool> Update(Channel channel);
        Task Delete(int channelId);
        Task<Unit> RemoveWatermark(Channel channel);
    }
}
