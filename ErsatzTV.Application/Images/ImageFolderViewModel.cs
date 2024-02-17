namespace ErsatzTV.Application.Images;

public record ImageFolderViewModel(
    int LibraryFolderId,
    string Name,
    string FullPath,
    int SubfolderCount,
    int ImageCount,
    Option<double> DurationSeconds);
