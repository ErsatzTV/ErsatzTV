using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddShowToCollectionHandler : IRequestHandler<AddShowToCollection, Either<BaseError, CollectionUpdateResult>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public AddShowToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
        }

        public Task<Either<BaseError, CollectionUpdateResult>> Handle(
            AddShowToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyAddTelevisionShowRequest(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<CollectionUpdateResult> ApplyAddTelevisionShowRequest(AddShowToCollection request)
        {
            var result = new CollectionUpdateResult();

            if (await _mediaCollectionRepository.AddMediaItem(request.CollectionId, request.ShowId))
            {
                result.ModifiedPlayoutIds =
                    await _mediaCollectionRepository.PlayoutIdsUsingCollection(request.CollectionId);
            }

            return result;
        }

        private async Task<Validation<BaseError, Unit>> Validate(AddShowToCollection request) =>
            (await CollectionMustExist(request), await ValidateShow(request))
            .Apply((_, _) => Unit.Default);

        private Task<Validation<BaseError, Unit>> CollectionMustExist(AddShowToCollection request) =>
            _mediaCollectionRepository.GetCollectionWithItems(request.CollectionId)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Task<Validation<BaseError, Unit>> ValidateShow(AddShowToCollection request) =>
            LoadTelevisionShow(request)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Show does not exist"));

        private Task<Option<Show>> LoadTelevisionShow(AddShowToCollection request) =>
            _televisionRepository.GetShow(request.ShowId);
    }
}
