namespace ErsatzTV.Application.Images;

public record UpdateImageFolderDuration(int LibraryFolderId, double? ImageFolderDuration) : IRequest<double?>;
