using System.Collections.Immutable;
using System.Globalization;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IMediaItemRepository
{
    Task<List<CultureInfo>> GetAllKnownCultures();
    Task<List<LanguageCodeAndName>> GetAllLanguageCodesAndNames();
    Task<List<int>> FlagFileNotFound(LibraryPath libraryPath, string path);
    Task<Unit> FlagNormal(MediaItem mediaItem);
    Task<Either<BaseError, Unit>> DeleteItems(List<int> mediaItemIds);
    Task<ImmutableHashSet<string>> GetAllTrashedItems(LibraryPath libraryPath);
    Task SetInterlacedRatio(MediaItem mediaItem, double interlacedRatio);
}
