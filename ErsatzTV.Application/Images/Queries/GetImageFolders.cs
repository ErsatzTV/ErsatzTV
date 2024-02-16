namespace ErsatzTV.Application.Images;

public record GetImageFolders(Option<int> LibraryFolderId) : IRequest<List<ImageFolderViewModel>>;
