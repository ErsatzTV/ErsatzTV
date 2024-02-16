using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Images;

public static class Mapper
{
    public static ImageFolderViewModel ProjectToViewModel(
        LibraryFolder libraryFolder,
        int childCount,
        int imageCount) =>
        new(
            libraryFolder.Id,
            new DirectoryInfo(libraryFolder.Path).Name,
            libraryFolder.Path,
            childCount,
            imageCount,
            libraryFolder.ImageFolderDuration?.DurationSeconds ?? Option<int>.None);
}
