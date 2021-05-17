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
    public class
        UpdateProgramScheduleHandler : IRequestHandler<UpdateProgramSchedule,
            Either<BaseError, ProgramScheduleViewModel>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IProgramScheduleRepository _programScheduleRepository;

        public UpdateProgramScheduleHandler(
            IProgramScheduleRepository programScheduleRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _programScheduleRepository = programScheduleRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, ProgramScheduleViewModel>> Handle(
            UpdateProgramSchedule request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(c => ApplyUpdateRequest(c, request))
                .Bind(v => v.ToEitherAsync());

        private async Task<ProgramScheduleViewModel> ApplyUpdateRequest(
            ProgramSchedule programSchedule,
            UpdateProgramSchedule update)
        {
            // we only need to rebuild playouts if the playback order has been modified
            bool needToRebuildPlayout =
                programSchedule.MediaCollectionPlaybackOrder != update.MediaCollectionPlaybackOrder;

            programSchedule.Name = update.Name;
            programSchedule.MediaCollectionPlaybackOrder = update.MediaCollectionPlaybackOrder;
            programSchedule.KeepMultiPartEpisodesTogether =
                update.MediaCollectionPlaybackOrder != PlaybackOrder.Chronological &&
                update.KeepMultiPartEpisodesTogether;
            await _programScheduleRepository.Update(programSchedule);

            if (needToRebuildPlayout)
            {
                foreach (Playout playout in programSchedule.Playouts)
                {
                    await _channel.WriteAsync(new BuildPlayout(playout.Id, true));
                }
            }

            return ProjectToViewModel(programSchedule);
        }

        private async Task<Validation<BaseError, ProgramSchedule>> Validate(UpdateProgramSchedule request) =>
            (await ProgramScheduleMustExist(request), ValidateName(request))
            .Apply((programScheduleToUpdate, _) => programScheduleToUpdate);

        private async Task<Validation<BaseError, ProgramSchedule>> ProgramScheduleMustExist(
            UpdateProgramSchedule updateProgramSchedule) =>
            (await _programScheduleRepository.GetWithPlayouts(updateProgramSchedule.ProgramScheduleId))
            .ToValidation<BaseError>("ProgramSchedule does not exist.");

        private Validation<BaseError, string> ValidateName(UpdateProgramSchedule updateProgramSchedule) =>
            updateProgramSchedule.NotEmpty(c => c.Name)
                .Bind(_ => updateProgramSchedule.NotLongerThan(50)(c => c.Name));
    }
}
