using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public class GetSimpleMediaCollectionCardsHandler : IRequestHandler<GetSimpleMediaCollectionCards,
        Either<BaseError, SimpleMediaCollectionCardResultsViewModel>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public GetSimpleMediaCollectionCardsHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public async Task<Either<BaseError, SimpleMediaCollectionCardResultsViewModel>> Handle(
            GetSimpleMediaCollectionCards request,
            CancellationToken cancellationToken) =>
            (await _mediaCollectionRepository.GetSimpleMediaCollectionWithItemsUntracked(request.Id))
            .ToEither(BaseError.New("Unable to load collection"))
            .Map(ProjectToViewModel);
    }
}
