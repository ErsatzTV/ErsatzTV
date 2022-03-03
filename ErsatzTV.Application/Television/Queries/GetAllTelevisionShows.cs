using ErsatzTV.Application.MediaItems;

namespace ErsatzTV.Application.Television;

public record GetAllTelevisionShows : IRequest<List<NamedMediaItemViewModel>>;