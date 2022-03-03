using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts;

public class DeletePlayoutHandler : IRequestHandler<DeletePlayout, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public DeletePlayoutHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, Unit>> Handle(
        DeletePlayout request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();

        Option<Playout> maybePlayout = await dbContext.Playouts
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync(p => p.Id == request.PlayoutId, cancellationToken);

        foreach (Playout playout in maybePlayout)
        {
            dbContext.Playouts.Remove(playout);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return maybePlayout
            .Map(_ => Unit.Default)
            .ToEither(BaseError.New($"Playout {request.PlayoutId} does not exist."));
    }
}