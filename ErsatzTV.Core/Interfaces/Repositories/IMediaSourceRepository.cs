﻿using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMediaSourceRepository
    {
        Task<LocalMediaSource> Add(LocalMediaSource localMediaSource);
        Task<PlexMediaSource> Add(PlexMediaSource plexMediaSource);
        Task<List<MediaSource>> GetAll();
        Task<List<PlexMediaSource>> GetAllPlex();
        Task<List<PlexLibrary>> GetPlexLibraries(int plexMediaSourceId);
        Task<List<PlexPathReplacement>> GetPlexPathReplacements(int plexMediaSourceId);
        Task<Option<PlexLibrary>> GetPlexLibrary(int plexLibraryId);
        Task<Option<MediaSource>> Get(int id);
        Task<Option<PlexMediaSource>> GetPlex(int id);
        Task<Option<PlexMediaSource>> GetPlexByLibraryId(int plexLibraryId);
        Task<List<PlexPathReplacement>> GetPlexPathReplacementsByLibraryId(int plexLibraryPathId);
        Task<int> CountMediaItems(int id);
        Task Update(LocalMediaSource localMediaSource);
        Task Update(PlexMediaSource plexMediaSource);
        Task Update(PlexLibrary plexMediaSourceLibrary);
        Task Delete(int mediaSourceId);
        Task<Unit> DeleteAllPlex();
        Task DisablePlexLibrarySync(List<int> libraryIds);
        Task EnablePlexLibrarySync(IEnumerable<int> libraryIds);
    }
}
