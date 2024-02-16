﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMusicVideoRepository
{
    Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> GetOrAdd(
        Artist artist,
        LibraryPath libraryPath,
        LibraryFolder libraryFolder,
        string path);

    Task<IEnumerable<string>> FindMusicVideoPaths(LibraryPath libraryPath);
    Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path);
    Task<bool> AddArtist(MusicVideoMetadata metadata, MusicVideoArtist artist);
    Task<bool> AddGenre(MusicVideoMetadata metadata, Genre genre);
    Task<bool> AddTag(MusicVideoMetadata metadata, Tag tag);
    Task<bool> AddStudio(MusicVideoMetadata metadata, Studio studio);
    Task<bool> AddDirector(MusicVideoMetadata metadata, Director director);
    Task<bool> RemoveArtist(MusicVideoArtist artist);
    Task<List<MusicVideoMetadata>> GetMusicVideosForCards(List<int> ids);
    Task<IEnumerable<string>> FindOrphanPaths(LibraryPath libraryPath);
    Task<int> GetMusicVideoCount(int artistId);
    Task<List<MusicVideoMetadata>> GetPagedMusicVideos(int artistId, int pageNumber, int pageSize);
}
