using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Libraries.Commands
{
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
            List<string> allPaths = await dbContext.LocalLibraries
                .Include(ll => ll.Paths)
                .Filter(ll => existingLibraryId == null || ll.Id != existingLibraryId)
                .ToListAsync()
                .Map(list => list.SelectMany(ll => ll.Paths).Map(lp => lp.Path).ToList());

            return Optional(localLibrary.Paths.Count(folder => allPaths.Any(f => AreSubPaths(f, folder.Path))))
                .Where(length => length == 0)
                .Map(_ => localLibrary)
                .ToValidation<BaseError>("Path must not belong to another library path");
        }
        
        private static bool AreSubPaths(string path1, string path2)
        {
            string one = path1 + Path.DirectorySeparatorChar;
            string two = path2 + Path.DirectorySeparatorChar;
            return one == two || one.StartsWith(two, StringComparison.OrdinalIgnoreCase) ||
                   two.StartsWith(one, StringComparison.OrdinalIgnoreCase);
        }
    }
}
