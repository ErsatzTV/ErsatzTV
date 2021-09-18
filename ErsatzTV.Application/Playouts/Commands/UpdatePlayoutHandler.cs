using System;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Playouts.Commands
{
    public class UpdatePlayoutHandler : IRequestHandler<UpdatePlayout, Either<BaseError, PlayoutNameViewModel>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public UpdatePlayoutHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<Either<BaseError, PlayoutNameViewModel>> Handle(
            UpdatePlayout request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Validation<BaseError, Playout> validation = await Validate(dbContext, request);
            return await validation.Apply(playout => ApplyUpdateRequest(dbContext, request, playout));
        }

        private static async Task<PlayoutNameViewModel> ApplyUpdateRequest(
            TvContext dbContext,
            UpdatePlayout request,
            Playout playout)
        {
            playout.DailyRebuildTime = null;

            foreach (TimeSpan dailyRebuildTime in request.DailyRebuildTime)
            {
                playout.DailyRebuildTime = dailyRebuildTime;
            }

            await dbContext.SaveChangesAsync();

            return new PlayoutNameViewModel(
                playout.Id,
                playout.Channel.Name,
                playout.Channel.Number,
                playout.ProgramSchedule.Name);
        }

        private static Task<Validation<BaseError, Playout>> Validate(TvContext dbContext, UpdatePlayout request) =>
            PlayoutMustExist(dbContext, request);

        private static Task<Validation<BaseError, Playout>> PlayoutMustExist(
            TvContext dbContext,
            UpdatePlayout updatePlayout) =>
            dbContext.Playouts
                .Include(p => p.Channel)
                .Include(p => p.ProgramSchedule)
                .SelectOneAsync(p => p.Id, p => p.Id == updatePlayout.PlayoutId)
                .Map(o => o.ToValidation<BaseError>("Playout does not exist."));
    }
}
