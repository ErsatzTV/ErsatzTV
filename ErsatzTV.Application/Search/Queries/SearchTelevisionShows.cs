using ErsatzTV.Application.MediaItems;

namespace ErsatzTV.Application.Search;

public record SearchTelevisionShows(string Query) : IRequest<List<NamedMediaItemViewModel>>;
