using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Libraries;

public record GetAllLocalLibraries : IRequest<List<LocalLibraryViewModel>>;