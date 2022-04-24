using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IPlexMovieRepository
{
    Task<bool> FlagNormal(PlexLibrary library, PlexMovie movie);
    Task<Option<int>> FlagUnavailable(PlexLibrary library, PlexMovie movie);
    Task<List<int>> FlagFileNotFound(PlexLibrary library, List<string> plexMovieKeys);
}
