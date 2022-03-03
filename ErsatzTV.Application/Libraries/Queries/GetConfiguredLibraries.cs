using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Libraries;

public record GetConfiguredLibraries : IRequest<List<LibraryViewModel>>;