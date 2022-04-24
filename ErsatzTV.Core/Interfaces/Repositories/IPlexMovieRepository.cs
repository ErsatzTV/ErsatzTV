using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IPlexMovieRepository
{
    Task<bool> FlagNormal(PlexLibrary library, PlexMovie movie);
    Task<bool> FlagUnavailable(PlexLibrary library, PlexMovie movie);
    Task<List<int>> FlagFileNotFound(PlexLibrary library, List<string> plexMovieKeys);
}
