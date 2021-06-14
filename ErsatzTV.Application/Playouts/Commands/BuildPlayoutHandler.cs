using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts.Commands
{
    public class BuildPlayoutHandler : MediatR.IRequestHandler<BuildPlayout, Either<BaseError, Unit>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;
        private readonly IPlayoutBuilder _playoutBuilder;

        public BuildPlayoutHandler(IDbContextFactory<TvContext> dbContextFactory, IPlayoutBuilder playoutBuilder)
        {
            _dbContextFactory = dbContextFactory;
            _playoutBuilder = playoutBuilder;
        }

        public async Task<Either<BaseError, Unit>> Handle(BuildPlayout request, CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Validation<BaseError, Playout> validation = await Validate(dbContext, request);
            return await validation.Apply(playout => ApplyUpdateRequest(dbContext, request, playout));
        }

        private async Task<Unit> ApplyUpdateRequest(TvContext dbContext, BuildPlayout request, Playout playout)
        {
            await _playoutBuilder.BuildPlayoutItems(playout, request.Rebuild);
            await dbContext.SaveChangesAsync();
            return Unit.Default;
        }

        private static Task<Validation<BaseError, Playout>> Validate(TvContext dbContext, BuildPlayout request) =>
            PlayoutMustExist(dbContext, request);

        private static Task<Validation<BaseError, Playout>> PlayoutMustExist(
            TvContext dbContext,
            BuildPlayout buildPlayout) =>
            dbContext.Playouts
                .Include(p => p.Channel)
                .Include(p => p.Items)
                .Include(p => p.ProgramScheduleAnchors)
                .ThenInclude(a => a.MediaItem)
                .Include(p => p.ProgramSchedule)
                .ThenInclude(ps => ps.Items)
                .ThenInclude(psi => psi.Collection)
                .Include(p => p.ProgramSchedule)
                .ThenInclude(ps => ps.Items)
                .ThenInclude(psi => psi.MediaItem)
                .SelectOneAsync(p => p.Id, p => p.Id == buildPlayout.PlayoutId)
                .Map(o => o.ToValidation<BaseError>("Playout does not exist."));
    }
}
