using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddEpisodeToCollectionHandler : MediatR.IRequestHandler<AddEpisodeToCollection,
            Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public AddEpisodeToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddEpisodeToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(ApplyAddTelevisionEpisodeRequest)
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddTelevisionEpisodeRequest(RequestParameters parameters)
        {
            if (parameters.Collection.MediaItems.All(s => s.Id != parameters.EpisodeToAdd.Id))
            {
                parameters.Collection.MediaItems.Add(parameters.EpisodeToAdd);
                await _mediaCollectionRepository.Update(parameters.Collection);
            }

            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>>
            Validate(AddEpisodeToCollection request) =>
            (await SimpleMediaCollectionMustExist(request), await ValidateEpisode(request))
            .Apply(
                (collectionToUpdate, episode) =>
                    new RequestParameters(collectionToUpdate, episode));

        private Task<Validation<BaseError, Collection>> SimpleMediaCollectionMustExist(
            AddEpisodeToCollection updateCollection) =>
            _mediaCollectionRepository.GetCollectionWithItems(updateCollection.CollectionId)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Task<Validation<BaseError, Episode>> ValidateEpisode(
            AddEpisodeToCollection request) =>
            LoadTelevisionEpisode(request)
                .Map(v => v.ToValidation<BaseError>("Episode does not exist"));

        private Task<Option<Episode>> LoadTelevisionEpisode(
            AddEpisodeToCollection request) =>
            _televisionRepository.GetEpisode(request.EpisodeId);

        private record RequestParameters(Collection Collection, Episode EpisodeToAdd);
    }
}
