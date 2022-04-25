using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IEmbyMovieRepository : IMediaServerMovieRepository<EmbyLibrary, EmbyMovie, EmbyItemEtag>
{
}
