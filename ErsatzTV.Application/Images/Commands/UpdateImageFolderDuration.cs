namespace ErsatzTV.Application.Images;

public record UpdateImageFolderDuration(int LibraryFolderId, int? ImageFolderDuration) : IRequest<int?>;
