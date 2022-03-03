using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Libraries;

public record GetLocalLibraryPaths(int LocalLibraryId) : IRequest<List<LocalLibraryPathViewModel>>;