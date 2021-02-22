using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Television.Mapper;

namespace ErsatzTV.Application.Television.Queries
{
    public class GetAllTelevisionShowsHandler : IRequestHandler<GetAllTelevisionShows, List<TelevisionShowViewModel>>
    {
        private readonly ITelevisionRepository _televisionRepository;

        public GetAllTelevisionShowsHandler(ITelevisionRepository televisionRepository) =>
            _televisionRepository = televisionRepository;

        public Task<List<TelevisionShowViewModel>> Handle(
            GetAllTelevisionShows request,
            CancellationToken cancellationToken) =>
            _televisionRepository.GetAllShows().Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
