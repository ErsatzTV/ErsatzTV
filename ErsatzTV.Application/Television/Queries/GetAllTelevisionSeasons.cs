using ErsatzTV.Application.MediaItems;

namespace ErsatzTV.Application.Television;

public record GetAllTelevisionSeasons : IRequest<List<NamedMediaItemViewModel>>;
