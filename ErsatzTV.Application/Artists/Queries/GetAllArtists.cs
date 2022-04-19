using ErsatzTV.Application.MediaItems;

namespace ErsatzTV.Application.Artists;

public record GetAllArtists : IRequest<List<NamedMediaItemViewModel>>;
