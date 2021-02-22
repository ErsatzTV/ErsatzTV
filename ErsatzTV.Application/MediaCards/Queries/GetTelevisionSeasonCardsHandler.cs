using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public class
        GetTelevisionSeasonCardsHandler : IRequestHandler<GetTelevisionSeasonCards, TelevisionSeasonCardResultsViewModel
        >
    {
        private readonly ITelevisionRepository _televisionRepository;

        public GetTelevisionSeasonCardsHandler(ITelevisionRepository televisionRepository) =>
            _televisionRepository = televisionRepository;

        public async Task<TelevisionSeasonCardResultsViewModel> Handle(
            GetTelevisionSeasonCards request,
            CancellationToken cancellationToken)
        {
            int count = await _televisionRepository.GetSeasonCount(request.TelevisionShowId);

            List<TelevisionSeasonCardViewModel> results = await _televisionRepository
                .GetPagedSeasons(request.TelevisionShowId, request.PageNumber, request.PageSize)
                .Map(list => list.Map(ProjectToViewModel).ToList());

            return new TelevisionSeasonCardResultsViewModel(count, results);
        }
    }
}
