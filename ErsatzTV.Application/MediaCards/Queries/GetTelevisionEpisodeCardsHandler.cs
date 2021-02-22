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
        GetTelevisionEpisodeCardsHandler : IRequestHandler<GetTelevisionEpisodeCards,
            TelevisionEpisodeCardResultsViewModel>
    {
        private readonly ITelevisionRepository _televisionRepository;

        public GetTelevisionEpisodeCardsHandler(ITelevisionRepository televisionRepository) =>
            _televisionRepository = televisionRepository;

        public async Task<TelevisionEpisodeCardResultsViewModel> Handle(
            GetTelevisionEpisodeCards request,
            CancellationToken cancellationToken)
        {
            int count = await _televisionRepository.GetEpisodeCount(request.TelevisionSeasonId);

            List<TelevisionEpisodeCardViewModel> results = await _televisionRepository
                .GetPagedEpisodes(request.TelevisionSeasonId, request.PageNumber, request.PageSize)
                .Map(list => list.Map(ProjectToViewModel).ToList());

            return new TelevisionEpisodeCardResultsViewModel(count, results);
        }
    }
}
