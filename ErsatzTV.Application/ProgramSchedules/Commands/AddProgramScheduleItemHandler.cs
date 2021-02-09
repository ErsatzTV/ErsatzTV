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
    public class AddProgramScheduleItemHandler : ProgramScheduleItemCommandBase, IRequestHandler<AddProgramScheduleItem,
        Either<BaseError, ProgramScheduleItemViewModel>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IProgramScheduleRepository _programScheduleRepository;

        public AddProgramScheduleItemHandler(
            IProgramScheduleRepository programScheduleRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
            : base(programScheduleRepository)
        {
            _programScheduleRepository = programScheduleRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, ProgramScheduleItemViewModel>> Handle(
            AddProgramScheduleItem request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(programSchedule => PersistItem(request, programSchedule))
                .Bind(v => v.ToEitherAsync());

        private async Task<ProgramScheduleItemViewModel> PersistItem(
            AddProgramScheduleItem request,
            ProgramSchedule programSchedule)
        {
            int nextIndex = programSchedule.Items.Select(i => i.Index).DefaultIfEmpty(0).Max() + 1;

            ProgramScheduleItem item = BuildItem(programSchedule, nextIndex, request);
            programSchedule.Items.Add(item);

            await _programScheduleRepository.Update(programSchedule);

            // rebuild any playouts that use this schedule
            foreach (Playout playout in programSchedule.Playouts)
            {
                await _channel.WriteAsync(new BuildPlayout(playout.Id, true));
            }

            return ProjectToViewModel(item);
        }

        private Task<Validation<BaseError, ProgramSchedule>> Validate(AddProgramScheduleItem request) =>
            ProgramScheduleMustExist(request.ProgramScheduleId)
                .BindT(programSchedule => PlayoutModeMustBeValid(request, programSchedule));
    }
}
