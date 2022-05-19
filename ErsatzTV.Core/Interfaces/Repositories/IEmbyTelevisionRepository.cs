using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IEmbyTelevisionRepository : IMediaServerTelevisionRepository<EmbyLibrary, EmbyShow, EmbySeason,
    EmbyEpisode, EmbyItemEtag>
{
}
