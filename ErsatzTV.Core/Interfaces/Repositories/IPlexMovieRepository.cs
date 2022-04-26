using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IPlexMovieRepository : IMediaServerMovieRepository<PlexLibrary, PlexMovie, PlexItemEtag>
{
}
