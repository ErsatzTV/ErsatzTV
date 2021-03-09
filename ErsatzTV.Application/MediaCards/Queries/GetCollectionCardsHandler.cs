using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
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

        public GetCollectionCardsHandler(IMediaCollectionRepository collectionRepository) =>
            _collectionRepository = collectionRepository;

        public Task<Either<BaseError, CollectionCardResultsViewModel>> Handle(
            GetCollectionCards request,
            CancellationToken cancellationToken) =>
            _collectionRepository.GetCollectionWithItemsUntracked(request.Id)
                .Map(c => c.ToEither(BaseError.New("Unable to load collection")))
                .MapT(ProjectToViewModel);
    }
}
