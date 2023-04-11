using ErsatzTV.Application.MediaItems;

namespace ErsatzTV.Application.Search;

public record SearchArtists(string Query) : IRequest<List<NamedMediaItemViewModel>>;
