using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class CreateSmartCollectionHandler :
    IRequestHandler<CreateSmartCollection, Either<BaseError, SmartCollectionViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ISearchTargets _searchTargets;

    public CreateSmartCollectionHandler(IDbContextFactory<TvContext> dbContextFactory, ISearchTargets searchTargets)
    {
        _dbContextFactory = dbContextFactory;
        _searchTargets = searchTargets;
    }

    public async Task<Either<BaseError, SmartCollectionViewModel>> Handle(
        CreateSmartCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, SmartCollection> validation = await Validate(dbContext, request);
        return await validation.Apply(c => PersistCollection(dbContext, c));
    }

    private async Task<SmartCollectionViewModel> PersistCollection(
        TvContext dbContext,
        SmartCollection smartCollection)
    {
        await dbContext.SmartCollections.AddAsync(smartCollection);
        await dbContext.SaveChangesAsync();
        _searchTargets.SearchTargetsChanged();
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
