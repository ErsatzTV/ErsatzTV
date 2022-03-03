using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.MediaItems.Mapper;

namespace ErsatzTV.Application.Television;

public class GetAllTelevisionShowsHandler : IRequestHandler<GetAllTelevisionShows, List<NamedMediaItemViewModel>>
{
    private readonly ITelevisionRepository _televisionRepository;

    public GetAllTelevisionShowsHandler(ITelevisionRepository televisionRepository) =>
        _televisionRepository = televisionRepository;

    public Task<List<NamedMediaItemViewModel>> Handle(
        GetAllTelevisionShows request,
        CancellationToken cancellationToken) =>
        _televisionRepository.GetAllShows().Map(list => list.Map(ProjectToViewModel).ToList());
}