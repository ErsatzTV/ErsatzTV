using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Libraries.Queries
{
    public record GetLocalLibraryPaths(int LocalLibraryId) : IRequest<List<LocalLibraryPathViewModel>>;
}
