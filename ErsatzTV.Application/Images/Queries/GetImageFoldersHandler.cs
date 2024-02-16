using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Images;

public class GetImageFoldersHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetImageFolders, List<ImageFolderViewModel>>
{
    public async Task<List<ImageFolderViewModel>> Handle(GetImageFolders request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // default to returning top-level folders
        int? parentId = null;

        // if a specific folder is requested, return its children
        foreach (int libraryFolderId in request.LibraryFolderId)
        {
            parentId = libraryFolderId;
        }

        List<LibraryFolder> folders = await dbContext.LibraryFolders
            .AsNoTracking()
            .Include(lf => lf.ImageFolderDuration)
            .Filter(lf => lf.LibraryPath.Library.MediaKind == LibraryMediaKind.Images)
            .Filter(lf => lf.ParentId == parentId)
            .ToListAsync(cancellationToken);

        var result = new List<ImageFolderViewModel>();

        foreach (LibraryFolder folder in folders)
        {
            // count direct children of this folder
            int childCount = await dbContext.LibraryFolders
                .AsNoTracking()
                .CountAsync(lf => lf.ParentId == folder.Id, cancellationToken);

            // count all child images (any level)
            int imageCount = await dbContext.MediaFiles
                .AsNoTracking()
                .CountAsync(mf => mf.Path.StartsWith(folder.Path), cancellationToken);

            result.Add(Mapper.ProjectToViewModel(folder, childCount, imageCount));
        }

        return result;
    }
}
