using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IPlexOtherVideoRepository : IMediaServerOtherVideoRepository<PlexLibrary, PlexOtherVideo, PlexItemEtag>
{
}
