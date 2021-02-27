using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaItems.Mapper;

namespace ErsatzTV.Application.Television.Queries
{
    public class
        GetAllTelevisionSeasonsHandler : IRequestHandler<GetAllTelevisionSeasons, List<NamedMediaItemViewModel>>
    {
        private readonly ITelevisionRepository _televisionRepository;

        public GetAllTelevisionSeasonsHandler(ITelevisionRepository televisionRepository) =>
            _televisionRepository = televisionRepository;

        public Task<List<NamedMediaItemViewModel>> Handle(
            GetAllTelevisionSeasons request,
            CancellationToken cancellationToken) =>
            _televisionRepository.GetAllSeasons().Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
