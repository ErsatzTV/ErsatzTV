using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IChannelRepository
{
    Task<Option<Channel>> Get(int id);
    Task<Option<Channel>> GetByNumber(string number);
    Task<List<Channel>> GetAll();
    Task<List<Channel>> GetAllForGuide();
    Task Delete(int channelId);
}