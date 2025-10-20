using ErsatzTV.Core;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class CreateDecoHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreateDeco, Either<BaseError, DecoViewModel>>
{
    public async Task<Either<BaseError, DecoViewModel>> Handle(
        CreateDeco request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Deco> validation = await Validate(dbContext, request);
        return await validation.Apply(profile => PersistDeco(dbContext, profile));
    }

    private static async Task<DecoViewModel> PersistDeco(TvContext dbContext, Deco deco)
    {
        await dbContext.Decos.AddAsync(deco);
        await dbContext.SaveChangesAsync();
        await dbContext.Entry(deco).Reference(d => d.DecoGroup).LoadAsync();
        return Mapper.ProjectToViewModel(deco);
    }

    private static async Task<Validation<BaseError, Deco>> Validate(TvContext dbContext, CreateDeco request) =>
        await ValidateDecoName(dbContext, request).MapT(name => new Deco
        {
            DecoGroupId = request.DecoGroupId,
            Name = name,
            BreakContent = [],
            DecoWatermarks = [],
            DecoGraphicsElements = []
        });

    private static async Task<Validation<BaseError, string>> ValidateDecoName(
        TvContext dbContext,
        CreateDeco request)
    {
        if (request.Name.Length > 50)
        {
            return BaseError.New($"Deco name \"{request.Name}\" is invalid");
        }

        Option<Deco> maybeExisting = await dbContext.Decos
            .FirstOrDefaultAsync(r => r.DecoGroupId == request.DecoGroupId && r.Name == request.Name)
            .Map(Optional);

        return maybeExisting.IsSome
            ? BaseError.New($"A deco named \"{request.Name}\" already exists in that deco group")
            : Success<BaseError, string>(request.Name);
    }
}
