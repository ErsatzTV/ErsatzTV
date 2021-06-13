using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts.Commands
{
    public class DeletePlayoutHandler : IRequestHandler<DeletePlayout, Option<BaseError>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public DeletePlayoutHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<Option<BaseError>> Handle(
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

            return maybePlayout.Match(
                _ => Option<BaseError>.None,
                () => BaseError.New($"Playout {request.PlayoutId} does not exist."));
        }
    }
}
