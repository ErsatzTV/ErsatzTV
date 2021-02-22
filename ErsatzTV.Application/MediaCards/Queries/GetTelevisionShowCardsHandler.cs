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
        GetTelevisionShowCardsHandler : IRequestHandler<GetTelevisionShowCards, TelevisionShowCardResultsViewModel>
    {
        private readonly ITelevisionRepository _televisionRepository;

        public GetTelevisionShowCardsHandler(ITelevisionRepository televisionRepository) =>
            _televisionRepository = televisionRepository;

        public async Task<TelevisionShowCardResultsViewModel> Handle(
            GetTelevisionShowCards request,
            CancellationToken cancellationToken)
        {
            int count = await _televisionRepository.GetShowCount();

            List<TelevisionShowCardViewModel> results = await _televisionRepository
                .GetPagedShows(request.PageNumber, request.PageSize)
                .Map(list => list.Map(ProjectToViewModel).ToList());

            return new TelevisionShowCardResultsViewModel(count, results);
        }
    }
}
