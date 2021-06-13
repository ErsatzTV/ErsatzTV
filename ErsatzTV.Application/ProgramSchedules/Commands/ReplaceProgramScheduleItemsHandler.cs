using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public class ReplaceProgramScheduleItemsHandler : ProgramScheduleItemCommandBase,
        IRequestHandler<ReplaceProgramScheduleItems, Either<BaseError, IEnumerable<ProgramScheduleItemViewModel>>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;

        public ReplaceProgramScheduleItemsHandler(
            IDbContextFactory<TvContext> dbContextFactory,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _dbContextFactory = dbContextFactory;
            _channel = channel;
        }

        public async Task<Either<BaseError, IEnumerable<ProgramScheduleItemViewModel>>> Handle(
            ReplaceProgramScheduleItems request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Validation<BaseError, ProgramSchedule> validation = await Validate(dbContext, request);
            return await validation.Apply(ps => PersistItems(dbContext, request, ps));
        }

        private async Task<IEnumerable<ProgramScheduleItemViewModel>> PersistItems(
            TvContext dbContext,
            ReplaceProgramScheduleItems request,
            ProgramSchedule programSchedule)
        {
            dbContext.RemoveRange(programSchedule.Items);
            programSchedule.Items = request.Items.Map(i => BuildItem(programSchedule, i.Index, i)).ToList();

            await dbContext.SaveChangesAsync();

            // rebuild any playouts that use this schedule
            foreach (Playout playout in programSchedule.Playouts)
            {
                await _channel.WriteAsync(new BuildPlayout(playout.Id, true));
            }

            return programSchedule.Items.Map(ProjectToViewModel);
        }

        private Task<Validation<BaseError, ProgramSchedule>> Validate(
            TvContext dbContext,
            ReplaceProgramScheduleItems request) =>
            ProgramScheduleMustExist(dbContext, request.ProgramScheduleId)
                .BindT(programSchedule => PlayoutModesMustBeValid(request, programSchedule))
                .BindT(programSchedule => CollectionTypesMustBeValid(request, programSchedule));

        private static Validation<BaseError, ProgramSchedule> PlayoutModesMustBeValid(
            ReplaceProgramScheduleItems request,
            ProgramSchedule programSchedule) =>
            request.Items.Map(item => PlayoutModeMustBeValid(item, programSchedule)).Sequence()
                .Map(_ => programSchedule);

        private Validation<BaseError, ProgramSchedule> CollectionTypesMustBeValid(
            ReplaceProgramScheduleItems request,
            ProgramSchedule programSchedule) =>
            request.Items.Map(item => CollectionTypeMustBeValid(item, programSchedule)).Sequence()
                .Map(_ => programSchedule);
    }
}
