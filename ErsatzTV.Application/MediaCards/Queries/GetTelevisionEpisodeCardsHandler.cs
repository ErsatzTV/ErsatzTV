using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
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
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public GetTelevisionEpisodeCardsHandler(
            ITelevisionRepository televisionRepository,
            IMediaSourceRepository mediaSourceRepository)
        {
            _televisionRepository = televisionRepository;
            _mediaSourceRepository = mediaSourceRepository;
        }

        public async Task<TelevisionEpisodeCardResultsViewModel> Handle(
            GetTelevisionEpisodeCards request,
            CancellationToken cancellationToken)
        {
            int count = await _televisionRepository.GetEpisodeCount(request.TelevisionSeasonId);

            Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
                .Map(list => list.HeadOrNone());

            Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
                .Map(list => list.HeadOrNone());

            List<TelevisionEpisodeCardViewModel> results = await _televisionRepository
                .GetPagedEpisodes(request.TelevisionSeasonId, request.PageNumber, request.PageSize)
                .Map(list => list.Map(e => ProjectToViewModel(e, maybeJellyfin, maybeEmby)).ToList());

            return new TelevisionEpisodeCardResultsViewModel(count, results);
        }
    }
}
