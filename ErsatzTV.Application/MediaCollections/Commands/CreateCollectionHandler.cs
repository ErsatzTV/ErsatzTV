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
    public class
        CreateCollectionHandler : IRequestHandler<CreateCollection, Either<BaseError, MediaCollectionViewModel>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public CreateCollectionHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<Either<BaseError, MediaCollectionViewModel>> Handle(
            CreateCollection request,
            CancellationToken cancellationToken) =>
            Validate(request).MapT(PersistCollection).Bind(v => v.ToEitherAsync());

        private Task<MediaCollectionViewModel> PersistCollection(Collection c) =>
            _mediaCollectionRepository.Add(c).Map(ProjectToViewModel);

        private Task<Validation<BaseError, Collection>> Validate(CreateCollection request) =>
            ValidateName(request).MapT(
                name => new Collection
                {
                    Name = name,
                    MediaItems = new List<MediaItem>()
                });

        private async Task<Validation<BaseError, string>> ValidateName(CreateCollection createCollection)
        {
            List<string> allNames = await _mediaCollectionRepository.GetAll()
                .Map(list => list.Map(c => c.Name).ToList());

            Validation<BaseError, string> result1 = createCollection.NotEmpty(c => c.Name)
                .Bind(_ => createCollection.NotLongerThan(50)(c => c.Name));

            var result2 = Optional(createCollection.Name)
                .Filter(name => !allNames.Contains(name))
                .ToValidation<BaseError>("Collection name must be unique");

            return (result1, result2).Apply((_, _) => createCollection.Name);
        }
    }
}
