using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class CreateCollectionHandler :
    IRequestHandler<CreateCollection, Either<BaseError, MediaCollectionViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public CreateCollectionHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, MediaCollectionViewModel>> Handle(
        CreateCollection request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        Validation<BaseError, Collection> validation = await Validate(dbContext, request);
        return await LanguageExtensions.Apply(validation, c => PersistCollection(dbContext, c));
    }

    private static async Task<MediaCollectionViewModel> PersistCollection(
        TvContext dbContext,
        Collection collection)
    {
        await dbContext.Collections.AddAsync(collection);
        await dbContext.SaveChangesAsync();
        return ProjectToViewModel(collection);
    }

    private static Task<Validation<BaseError, Collection>> Validate(
        TvContext dbContext,
        CreateCollection request) =>
        ValidateName(dbContext, request).MapT(
            name => new Collection
            {
                Name = name,
                MediaItems = new List<MediaItem>()
            });

    private static async Task<Validation<BaseError, string>> ValidateName(
        TvContext dbContext,
        CreateCollection createCollection)
    {
        List<string> allNames = await dbContext.Collections
            .Map(c => c.Name)
            .ToListAsync();

        Validation<BaseError, string> result1 = createCollection.NotEmpty(c => c.Name)
            .Bind(_ => createCollection.NotLongerThan(50)(c => c.Name));

        var result2 = Optional(createCollection.Name)
            .Where(name => !allNames.Contains(name))
            .ToValidation<BaseError>("Collection name must be unique");

        return (result1, result2).Apply((_, _) => createCollection.Name);
    }
}
