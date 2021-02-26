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
        private readonly ICollectionRepository _collectionRepository;

        public GetCollectionCardsHandler(ICollectionRepository collectionRepository) =>
            _collectionRepository = collectionRepository;

        public async Task<Either<BaseError, CollectionCardResultsViewModel>> Handle(
            GetCollectionCards request,
            CancellationToken cancellationToken) =>
            (await _collectionRepository.GetWithItemsUntracked(request.Id))
            .ToEither(BaseError.New("Unable to load collection"))
            .Map(ProjectToViewModel);
    }
}
