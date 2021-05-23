using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public class GetCollectionCardsHandler : IRequestHandler<GetCollectionCards,
        Either<BaseError, CollectionCardResultsViewModel>>
    {
        private readonly IMediaCollectionRepository _collectionRepository;
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public GetCollectionCardsHandler(
            IMediaCollectionRepository collectionRepository,
            IMediaSourceRepository mediaSourceRepository)
        {
            _collectionRepository = collectionRepository;
            _mediaSourceRepository = mediaSourceRepository;
        }

        public async Task<Either<BaseError, CollectionCardResultsViewModel>> Handle(
            GetCollectionCards request,
            CancellationToken cancellationToken)
        {
            Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
                .Map(list => list.HeadOrNone());

            Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
                .Map(list => list.HeadOrNone());

            return await _collectionRepository
                .GetCollectionWithItemsUntracked(request.Id)
                .Map(c => c.ToEither(BaseError.New("Unable to load collection")))
                .MapT(c => ProjectToViewModel(c, maybeJellyfin, maybeEmby));
        }
    }
}
