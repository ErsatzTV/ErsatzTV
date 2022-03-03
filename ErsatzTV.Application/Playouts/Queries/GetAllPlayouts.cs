using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Playouts;

public record GetAllPlayouts : IRequest<List<PlayoutNameViewModel>>;