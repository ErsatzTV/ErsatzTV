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
        AddTelevisionEpisodeToSimpleMediaCollectionHandler : MediatR.IRequestHandler<
            AddTelevisionEpisodeToSimpleMediaCollection,
            Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public AddTelevisionEpisodeToSimpleMediaCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddTelevisionEpisodeToSimpleMediaCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(ApplyAddTelevisionEpisodeRequest)
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddTelevisionEpisodeRequest(RequestParameters parameters)
        {
            if (parameters.Collection.TelevisionEpisodes.All(s => s.Id != parameters.EpisodeToAdd.Id))
            {
                parameters.Collection.TelevisionEpisodes.Add(parameters.EpisodeToAdd);
                await _mediaCollectionRepository.Update(parameters.Collection);
            }

            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>>
            Validate(AddTelevisionEpisodeToSimpleMediaCollection request) =>
            (await SimpleMediaCollectionMustExist(request), await ValidateEpisode(request))
            .Apply(
                (simpleMediaCollectionToUpdate, episode) =>
                    new RequestParameters(simpleMediaCollectionToUpdate, episode));

        private Task<Validation<BaseError, SimpleMediaCollection>> SimpleMediaCollectionMustExist(
            AddTelevisionEpisodeToSimpleMediaCollection updateSimpleMediaCollection) =>
            _mediaCollectionRepository.GetSimpleMediaCollectionWithItems(updateSimpleMediaCollection.MediaCollectionId)
                .Map(v => v.ToValidation<BaseError>("SimpleMediaCollection does not exist."));

        private Task<Validation<BaseError, TelevisionEpisodeMediaItem>> ValidateEpisode(
            AddTelevisionEpisodeToSimpleMediaCollection request) =>
            LoadTelevisionEpisode(request)
                .Map(v => v.ToValidation<BaseError>("TelevisionEpisode does not exist"));

        private Task<Option<TelevisionEpisodeMediaItem>> LoadTelevisionEpisode(
            AddTelevisionEpisodeToSimpleMediaCollection request) =>
            _televisionRepository.GetEpisode(request.TelevisionEpisodeId);

        private record RequestParameters(SimpleMediaCollection Collection, TelevisionEpisodeMediaItem EpisodeToAdd);
    }
}
