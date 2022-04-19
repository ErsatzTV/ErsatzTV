using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMediaSourceRepository
{
    Task<PlexMediaSource> Add(PlexMediaSource plexMediaSource);
    Task<List<PlexMediaSource>> GetAllPlex();
    Task<List<PlexLibrary>> GetPlexLibraries(int plexMediaSourceId);
    Task<List<PlexPathReplacement>> GetPlexPathReplacements(int plexMediaSourceId);
    Task<Option<PlexLibrary>> GetPlexLibrary(int plexLibraryId);
    Task<Option<PlexMediaSource>> GetPlex(int id);
    Task<Option<PlexMediaSource>> GetPlexByLibraryId(int plexLibraryId);
    Task<List<PlexPathReplacement>> GetPlexPathReplacementsByLibraryId(int plexLibraryPathId);

    Task Update(
        PlexMediaSource plexMediaSource,
        List<PlexConnection> toAdd,
        List<PlexConnection> toDelete);

    Task<List<int>> UpdateLibraries(
        int plexMediaSourceId,
        List<PlexLibrary> toAdd,
        List<PlexLibrary> toDelete);

    Task<List<int>> UpdateLibraries(
        int jellyfinMediaSourceId,
        List<JellyfinLibrary> toAdd,
        List<JellyfinLibrary> toDelete);

    Task<List<int>> UpdateLibraries(
        int embyMediaSourceId,
        List<EmbyLibrary> toAdd,
        List<EmbyLibrary> toDelete);

    Task<Unit> UpdatePathReplacements(
        int plexMediaSourceId,
        List<PlexPathReplacement> toAdd,
        List<PlexPathReplacement> toUpdate,
        List<PlexPathReplacement> toDelete);

    Task<List<int>> DeleteAllPlex();
    Task<List<int>> DeletePlex(PlexMediaSource plexMediaSource);
    Task<List<int>> DisablePlexLibrarySync(List<int> libraryIds);
    Task EnablePlexLibrarySync(IEnumerable<int> libraryIds);

    Task<Unit> UpsertJellyfin(string address, string serverName, string operatingSystem);
    Task<List<JellyfinMediaSource>> GetAllJellyfin();
    Task<Option<JellyfinMediaSource>> GetJellyfin(int id);
    Task<List<JellyfinLibrary>> GetJellyfinLibraries(int jellyfinMediaSourceId);
    Task<Unit> EnableJellyfinLibrarySync(IEnumerable<int> libraryIds);
    Task<List<int>> DisableJellyfinLibrarySync(List<int> libraryIds);
    Task<Option<JellyfinLibrary>> GetJellyfinLibrary(int jellyfinLibraryId);
    Task<Option<JellyfinMediaSource>> GetJellyfinByLibraryId(int jellyfinLibraryId);
    Task<List<JellyfinPathReplacement>> GetJellyfinPathReplacements(int jellyfinMediaSourceId);
    Task<List<JellyfinPathReplacement>> GetJellyfinPathReplacementsByLibraryId(int jellyfinLibraryPathId);

    Task<Unit> UpdatePathReplacements(
        int jellyfinMediaSourceId,
        List<JellyfinPathReplacement> toAdd,
        List<JellyfinPathReplacement> toUpdate,
        List<JellyfinPathReplacement> toDelete);

    Task<List<int>> DeleteAllJellyfin();

    Task<Unit> UpsertEmby(string address, string serverName, string operatingSystem);
    Task<List<EmbyMediaSource>> GetAllEmby();
    Task<Option<EmbyMediaSource>> GetEmby(int id);
    Task<Option<EmbyMediaSource>> GetEmbyByLibraryId(int embyLibraryId);
    Task<Option<EmbyLibrary>> GetEmbyLibrary(int embyLibraryId);
    Task<List<EmbyLibrary>> GetEmbyLibraries(int embyMediaSourceId);
    Task<List<EmbyPathReplacement>> GetEmbyPathReplacements(int embyMediaSourceId);
    Task<List<EmbyPathReplacement>> GetEmbyPathReplacementsByLibraryId(int embyLibraryPathId);

    Task<Unit> UpdatePathReplacements(
        int embyMediaSourceId,
        List<EmbyPathReplacement> toAdd,
        List<EmbyPathReplacement> toUpdate,
        List<EmbyPathReplacement> toDelete);

    Task<List<int>> DeleteAllEmby();
    Task<Unit> EnableEmbyLibrarySync(IEnumerable<int> libraryIds);
    Task<List<int>> DisableEmbyLibrarySync(List<int> libraryIds);
}
