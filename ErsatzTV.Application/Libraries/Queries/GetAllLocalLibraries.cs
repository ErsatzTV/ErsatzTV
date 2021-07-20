using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Libraries.Queries
{
    public record GetAllLocalLibraries : IRequest<List<LocalLibraryViewModel>>;
}
