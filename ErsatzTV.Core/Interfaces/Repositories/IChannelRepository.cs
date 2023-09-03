using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IChannelRepository
{
    Task<Option<Channel>> GetChannel(int id);
    Task<Option<Channel>> GetByNumber(string number);
    Task<List<Channel>> GetAll();
}
