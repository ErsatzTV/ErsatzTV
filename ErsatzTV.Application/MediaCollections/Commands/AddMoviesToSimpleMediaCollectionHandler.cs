using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddMoviesToSimpleMediaCollectionHandler : MediatR.IRequestHandler<AddMoviesToSimpleMediaCollection,
            Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly IMediaItemRepository _mediaItemRepository;

        public AddMoviesToSimpleMediaCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            IMediaItemRepository mediaItemRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _mediaItemRepository = mediaItemRepository;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddMoviesToSimpleMediaCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(ApplyAddMoviesRequest)
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddMoviesRequest(RequestParameters parameters)
        {
            foreach (MovieMediaItem item in parameters.MoviesToAdd.Where(
                item => parameters.Collection.Movies.All(i => i.Id != item.Id)))
            {
                parameters.Collection.Movies.Add(item);
            }

            await _mediaCollectionRepository.Update(parameters.Collection);

            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>>
            Validate(AddMoviesToSimpleMediaCollection request) =>
            (await SimpleMediaCollectionMustExist(request), await ValidateMovies(request))
            .Apply(
                (simpleMediaCollectionToUpdate, moviesToAdd) =>
                    new RequestParameters(simpleMediaCollectionToUpdate, moviesToAdd));

        private Task<Validation<BaseError, SimpleMediaCollection>> SimpleMediaCollectionMustExist(
            AddMoviesToSimpleMediaCollection updateSimpleMediaCollection) =>
            _mediaCollectionRepository.GetSimpleMediaCollection(updateSimpleMediaCollection.MediaCollectionId)
                .Map(v => v.ToValidation<BaseError>("SimpleMediaCollection does not exist."));

        private Task<Validation<BaseError, List<MovieMediaItem>>> ValidateMovies(
            AddMoviesToSimpleMediaCollection request) =>
            LoadAllMovies(request)
                .Map(v => v.ToValidation<BaseError>("MovieMediaItem does not exist"));

        private async Task<Option<List<MovieMediaItem>>> LoadAllMovies(AddMoviesToSimpleMediaCollection request)
        {
            var items = (await request.MovieIds.Map(async id => await _mediaItemRepository.Get(id)).Sequence())
                .ToList();
            if (items.Any(i => i.IsNone))
            {
                return None;
            }

            return items.Somes().OfType<MovieMediaItem>().ToList();
        }

        private record RequestParameters(SimpleMediaCollection Collection, List<MovieMediaItem> MoviesToAdd);
    }
}
