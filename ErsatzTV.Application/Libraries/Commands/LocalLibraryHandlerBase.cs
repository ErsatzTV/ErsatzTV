using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Libraries;

public abstract class LocalLibraryHandlerBase
{
    protected static Task<Validation<BaseError, LocalLibrary>> NameMustBeValid(
        ILocalLibraryRequest request,
        LocalLibrary localLibrary) =>
        request.NotEmpty(c => c.Name)
            .Bind(_ => request.NotLongerThan(50)(c => c.Name))
            .Map(_ => localLibrary).AsTask();

    protected static async Task<Validation<BaseError, LocalLibrary>> PathsMustBeValid(
        TvContext dbContext,
        LocalLibrary localLibrary,
        int? existingLibraryId = null)
    {
        List<LocalPath> allPaths = await dbContext.LocalLibraries
            .Include(ll => ll.Paths)
            .Filter(ll => existingLibraryId == null || ll.Id != existingLibraryId)
            .ToListAsync()
            .Map(list => list.SelectMany(ll => ll.Paths.Map(lp => new LocalPath(ll.MediaKind, lp.Path))).ToList());

        var localPaths = localLibrary.Paths.Map(lp => new LocalPath(localLibrary.MediaKind, lp.Path)).ToList();

        return Optional(localPaths.Count(folder => allPaths.Any(f => AreSubPaths(f, folder))))
            .Where(length => length == 0)
            .Map(_ => localLibrary)
            .ToValidation<BaseError>("Path must not belong to another library path");
    }

    private static bool AreSubPaths(LocalPath path1, LocalPath path2)
    {
        string one = path1.Path + Path.DirectorySeparatorChar;
        string two = path2.Path + Path.DirectorySeparatorChar;

        bool isConflict = one == two || one.StartsWith(two, StringComparison.OrdinalIgnoreCase) ||
                          two.StartsWith(one, StringComparison.OrdinalIgnoreCase);

        // Images and OtherVideos do not conflict
        if (isConflict)
        {
            bool imagesAndOtherVideos = (path1.MediaKind is LibraryMediaKind.Images &&
                                         path2.MediaKind is LibraryMediaKind.OtherVideos)
                                        || (path2.MediaKind is LibraryMediaKind.Images &&
                                            path1.MediaKind is LibraryMediaKind.OtherVideos);

            if (imagesAndOtherVideos)
            {
                isConflict = false;
            }
        }

        return isConflict;
    }

    protected record LocalPath(LibraryMediaKind MediaKind, string Path);
}
