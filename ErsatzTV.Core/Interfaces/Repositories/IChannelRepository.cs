using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IChannelRepository
    {
        public Task<Channel> Add(Channel channel);
        public Task<Option<Channel>> Get(int id);
        public Task<Option<Channel>> GetByNumber(int number);
        public Task<List<Channel>> GetAll();
        public Task<List<Channel>> GetAllForGuide();
        public Task Update(Channel channel);
        public Task Delete(int channelId);
    }
}
