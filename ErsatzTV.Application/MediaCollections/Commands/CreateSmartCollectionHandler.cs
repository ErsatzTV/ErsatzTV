using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class CreateSmartCollectionHandler :
    IRequestHandler<CreateSmartCollection, Either<BaseError, SmartCollectionViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public CreateSmartCollectionHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, SmartCollectionViewModel>> Handle(
        CreateSmartCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        Validation<BaseError, SmartCollection> validation = await Validate(dbContext, request);
        return await LanguageExtensions.Apply(validation, c => PersistCollection(dbContext, c));
    }

    private static async Task<SmartCollectionViewModel> PersistCollection(
        TvContext dbContext,
        SmartCollection smartCollection)
    {
        await dbContext.SmartCollections.AddAsync(smartCollection);
        await dbContext.SaveChangesAsync();
        return ProjectToViewModel(smartCollection);
    }

    private static Task<Validation<BaseError, SmartCollection>> Validate(
        TvContext dbContext,
        CreateSmartCollection request) =>
        ValidateName(dbContext, request).MapT(
            name => new SmartCollection
            {
                Name = name,
                Query = request.Query
            });

    private static async Task<Validation<BaseError, string>> ValidateName(
        TvContext dbContext,
        CreateSmartCollection createSmartCollection)
    {
        List<string> allNames = await dbContext.SmartCollections
            .Map(c => c.Name)
            .ToListAsync();

        Validation<BaseError, string> result1 = createSmartCollection.NotEmpty(c => c.Name)
            .Bind(_ => createSmartCollection.NotLongerThan(50)(c => c.Name));

        var result2 = Optional(createSmartCollection.Name)
            .Where(name => !allNames.Contains(name))
            .ToValidation<BaseError>("SmartCollection name must be unique");

        return (result1, result2).Apply((_, _) => createSmartCollection.Name);
    }
}