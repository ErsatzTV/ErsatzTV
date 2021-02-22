using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Television.Mapper;

namespace ErsatzTV.Application.Television.Queries
{
    public class
        GetTelevisionSeasonByIdHandler : IRequestHandler<GetTelevisionSeasonById, Option<TelevisionSeasonViewModel>>
    {
        private readonly ITelevisionRepository _televisionRepository;

        public GetTelevisionSeasonByIdHandler(ITelevisionRepository televisionRepository) =>
            _televisionRepository = televisionRepository;

        public Task<Option<TelevisionSeasonViewModel>> Handle(
            GetTelevisionSeasonById request,
            CancellationToken cancellationToken) =>
            _televisionRepository.GetSeason(request.SeasonId)
                .MapT(ProjectToViewModel);
    }
}
