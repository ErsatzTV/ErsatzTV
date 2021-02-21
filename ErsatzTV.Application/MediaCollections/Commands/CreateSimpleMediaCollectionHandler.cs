using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaCollections.Mapper;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class CreateSimpleMediaCollectionHandler : IRequestHandler<CreateSimpleMediaCollection,
        Either<BaseError, MediaCollectionViewModel>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public CreateSimpleMediaCollectionHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<Either<BaseError, MediaCollectionViewModel>> Handle(
            CreateSimpleMediaCollection request,
            CancellationToken cancellationToken) =>
            Validate(request).MapT(PersistCollection).Bind(v => v.ToEitherAsync());

        private Task<MediaCollectionViewModel> PersistCollection(SimpleMediaCollection c) =>
            _mediaCollectionRepository.Add(c).Map(ProjectToViewModel);

        private Task<Validation<BaseError, SimpleMediaCollection>> Validate(CreateSimpleMediaCollection request) =>
            ValidateName(request).MapT(
                name => new SimpleMediaCollection
                {
                    Name = name,
                    Movies = new List<MovieMediaItem>(),
                    TelevisionShows = new List<TelevisionShow>(),
                    TelevisionEpisodes = new List<TelevisionEpisodeMediaItem>(),
                    TelevisionSeasons = new List<TelevisionSeason>()
                });

        private async Task<Validation<BaseError, string>> ValidateName(CreateSimpleMediaCollection createCollection)
        {
            List<string> allNames = await _mediaCollectionRepository.GetSimpleMediaCollections()
                .Map(list => list.Map(c => c.Name).ToList());

            Validation<BaseError, string> result1 = createCollection.NotEmpty(c => c.Name)
                .Bind(_ => createCollection.NotLongerThan(50)(c => c.Name));

            var result2 = Optional(createCollection.Name)
                .Filter(name => !allNames.Contains(name))
                .ToValidation<BaseError>("Media collection name must be unique");

            return (result1, result2).Apply((_, _) => createCollection.Name);
        }
    }
}
