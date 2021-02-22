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
        RemoveItemsFromSimpleMediaCollectionHandler : MediatR.IRequestHandler<
            RemoveItemsFromSimpleMediaCollection,
            Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public RemoveItemsFromSimpleMediaCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<Either<BaseError, Unit>> Handle(
            RemoveItemsFromSimpleMediaCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(collection => ApplyAddTelevisionEpisodeRequest(request, collection))
                .Bind(v => v.ToEitherAsync());

        private Task<Unit> ApplyAddTelevisionEpisodeRequest(
            RemoveItemsFromSimpleMediaCollection request,
            SimpleMediaCollection collection)
        {
            var moviesToRemove = collection.Movies
                .Filter(m => request.MovieIds.Contains(m.Id))
                .ToList();

            moviesToRemove.ForEach(m => collection.Movies.Remove(m));

            var showsToRemove = collection.TelevisionShows
                .Filter(s => request.TelevisionShowIds.Contains(s.Id))
                .ToList();

            showsToRemove.ForEach(s => collection.TelevisionShows.Remove(s));

            var seasonsToRemove = collection.TelevisionSeasons
                .Filter(s => request.TelevisionSeasonIds.Contains(s.Id))
                .ToList();

            seasonsToRemove.ForEach(s => collection.TelevisionSeasons.Remove(s));

            var episodesToRemove = collection.TelevisionEpisodes
                .Filter(e => request.TelevisionEpisodeIds.Contains(e.Id))
                .ToList();

            episodesToRemove.ForEach(e => collection.TelevisionEpisodes.Remove(e));

            if (moviesToRemove.Any() || showsToRemove.Any() || seasonsToRemove.Any() || episodesToRemove.Any())
            {
                return _mediaCollectionRepository.Update(collection).ToUnit();
            }

            return Task.FromResult(Unit.Default);
        }

        private Task<Validation<BaseError, SimpleMediaCollection>> Validate(
            RemoveItemsFromSimpleMediaCollection request) =>
            SimpleMediaCollectionMustExist(request);

        private Task<Validation<BaseError, SimpleMediaCollection>> SimpleMediaCollectionMustExist(
            RemoveItemsFromSimpleMediaCollection updateSimpleMediaCollection) =>
            _mediaCollectionRepository.GetSimpleMediaCollectionWithItems(updateSimpleMediaCollection.MediaCollectionId)
                .Map(v => v.ToValidation<BaseError>("SimpleMediaCollection does not exist."));
    }
}
