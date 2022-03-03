using System.Collections.Generic;
using ErsatzTV.Application.MediaItems;
using MediatR;

namespace ErsatzTV.Application.Artists;

public record GetAllArtists : IRequest<List<NamedMediaItemViewModel>>;