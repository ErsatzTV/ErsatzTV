using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMediaItemRepository
{
    Task<List<string>> GetAllLanguageCodes();
    Task<List<int>> FlagFileNotFound(LibraryPath libraryPath, string path);
    Task<Unit> FlagNormal(MediaItem mediaItem);
    Task<Either<BaseError, Unit>> DeleteItems(List<int> mediaItemIds);
}
