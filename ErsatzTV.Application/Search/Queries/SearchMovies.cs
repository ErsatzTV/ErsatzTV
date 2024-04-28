using ErsatzTV.Application.MediaItems;

namespace ErsatzTV.Application.Search;

public record SearchMovies(string Query) : IRequest<List<NamedMediaItemViewModel>>;
