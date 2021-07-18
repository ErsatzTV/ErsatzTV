using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class CreateMultiCollectionHandler :
        IRequestHandler<CreateMultiCollection, Either<BaseError, MultiCollectionViewModel>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public CreateMultiCollectionHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<Either<BaseError, MultiCollectionViewModel>> Handle(
            CreateMultiCollection request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Validation<BaseError, MultiCollection> validation = await Validate(dbContext, request);
            return await validation.Apply(c => PersistCollection(dbContext, c));
        }

        private static async Task<MultiCollectionViewModel> PersistCollection(
            TvContext dbContext,
            MultiCollection multiCollection)
        {
            await dbContext.MultiCollections.AddAsync(multiCollection);
            await dbContext.SaveChangesAsync();
            await dbContext.Entry(multiCollection)
                .Collection(c => c.MultiCollectionItems)
                .Query()
                .Include(i => i.Collection)
                .LoadAsync();
            return ProjectToViewModel(multiCollection);
        }

        private static Task<Validation<BaseError, MultiCollection>> Validate(
            TvContext dbContext,
            CreateMultiCollection request) =>
            ValidateName(dbContext, request).MapT(
                name => new MultiCollection
                {
                    Name = name,
                    MultiCollectionItems = request.Items.Map(i => new MultiCollectionItem
                    {
                        CollectionId = i.CollectionId,
                        ScheduleAsGroup = i.ScheduleAsGroup,
                        PlaybackOrder = i.PlaybackOrder
                    }).ToList()
                });

        private static async Task<Validation<BaseError, string>> ValidateName(
            TvContext dbContext,
            CreateMultiCollection createMultiCollection)
        {
            List<string> allNames = await dbContext.MultiCollections
                .Map(c => c.Name)
                .ToListAsync();

            Validation<BaseError, string> result1 = createMultiCollection.NotEmpty(c => c.Name)
                .Bind(_ => createMultiCollection.NotLongerThan(50)(c => c.Name));

            var result2 = Optional(createMultiCollection.Name)
                .Filter(name => !allNames.Contains(name))
                .ToValidation<BaseError>("MultiCollection name must be unique");

            return (result1, result2).Apply((_, _) => createMultiCollection.Name);
        }
    }
}
