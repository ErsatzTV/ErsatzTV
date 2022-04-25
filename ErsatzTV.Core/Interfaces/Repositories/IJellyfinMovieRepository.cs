using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface
    IJellyfinMovieRepository : IMediaServerMovieRepository<JellyfinLibrary, JellyfinMovie, JellyfinItemEtag>
{
}
