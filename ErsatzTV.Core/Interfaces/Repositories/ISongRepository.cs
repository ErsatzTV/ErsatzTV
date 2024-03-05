﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface ISongRepository
{
    Task<Either<BaseError, MediaItemScanResult<Song>>> GetOrAdd(
        LibraryPath libraryPath,
        LibraryFolder libraryFolder,
        string path);

    Task<IEnumerable<string>> FindSongPaths(LibraryPath libraryPath);
    Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path);
    Task<bool> AddGenre(SongMetadata metadata, Genre genre);
    Task<bool> AddTag(SongMetadata metadata, Tag tag);
    Task<List<SongMetadata>> GetSongsForCards(List<int> ids);
}
