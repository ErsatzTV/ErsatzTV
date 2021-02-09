using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public class ReplaceProgramScheduleItemsHandler : ProgramScheduleItemCommandBase,
        IRequestHandler<ReplaceProgramScheduleItems,
            Either<BaseError, IEnumerable<ProgramScheduleItemViewModel>>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IProgramScheduleRepository _programScheduleRepository;

        public ReplaceProgramScheduleItemsHandler(
            IProgramScheduleRepository programScheduleRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
            : base(programScheduleRepository)
        {
            _programScheduleRepository = programScheduleRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, IEnumerable<ProgramScheduleItemViewModel>>> Handle(
            ReplaceProgramScheduleItems request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(programSchedule => PersistItems(request, programSchedule))
                .Bind(v => v.ToEitherAsync());

        private async Task<IEnumerable<ProgramScheduleItemViewModel>> PersistItems(
            ReplaceProgramScheduleItems request,
            ProgramSchedule programSchedule)
        {
            programSchedule.Items = request.Items.Map(i => BuildItem(programSchedule, i.Index, i)).ToList();

            await _programScheduleRepository.Update(programSchedule);

            // rebuild any playouts that use this schedule
            foreach (Playout playout in programSchedule.Playouts)
            {
                await _channel.WriteAsync(new BuildPlayout(playout.Id, true));
            }

            return programSchedule.Items.Map(ProjectToViewModel);
        }

        private Task<Validation<BaseError, ProgramSchedule>> Validate(ReplaceProgramScheduleItems request) =>
            ProgramScheduleMustExist(request.ProgramScheduleId)
                .BindT(programSchedule => PlayoutModesMustBeValid(request, programSchedule));

        private Validation<BaseError, ProgramSchedule> PlayoutModesMustBeValid(
            ReplaceProgramScheduleItems request,
            ProgramSchedule programSchedule) =>
            request.Items.Map(item => PlayoutModeMustBeValid(item, programSchedule)).Sequence()
                .Map(_ => programSchedule);
    }
}
