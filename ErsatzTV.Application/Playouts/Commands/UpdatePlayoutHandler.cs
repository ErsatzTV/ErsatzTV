using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static LanguageExt.Prelude;
using static ErsatzTV.Application.Playouts.Mapper;
using Channel = ErsatzTV.Core.Domain.Channel;

namespace ErsatzTV.Application.Playouts.Commands
{
    public class UpdatePlayoutHandler : IRequestHandler<UpdatePlayout, Either<BaseError, PlayoutViewModel>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IChannelRepository _channelRepository;
        private readonly IPlayoutRepository _playoutRepository;
        private readonly IProgramScheduleRepository _programScheduleRepository;

        public UpdatePlayoutHandler(
            IPlayoutRepository playoutRepository,
            IChannelRepository channelRepository,
            IProgramScheduleRepository programScheduleRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _playoutRepository = playoutRepository;
            _channelRepository = channelRepository;
            _programScheduleRepository = programScheduleRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, PlayoutViewModel>> Handle(
            UpdatePlayout request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(c => ApplyUpdateRequest(c, request))
                .Bind(v => v.ToEitherAsync());

        private async Task<PlayoutViewModel> ApplyUpdateRequest(Playout p, UpdatePlayout update)
        {
            p.ChannelId = update.ChannelId;
            p.ProgramScheduleId = update.ProgramScheduleId;
            p.ProgramSchedulePlayoutType = update.ProgramSchedulePlayoutType;
            await _playoutRepository.Update(p);
            await _channel.WriteAsync(new BuildPlayout(p.Id));
            return ProjectToViewModel(p);
        }

        private async Task<Validation<BaseError, Playout>> Validate(UpdatePlayout request) =>
            (await PlayoutMustExist(request), await ChannelMustExist(request), await ProgramScheduleMustExist(request),
                ValidatePlayoutType(request))
            .Apply(
                (playoutToUpdate, _, _, _) => playoutToUpdate);

        private async Task<Validation<BaseError, Playout>> PlayoutMustExist(UpdatePlayout updatePlayout) =>
            (await _playoutRepository.Get(updatePlayout.PlayoutId))
            .ToValidation<BaseError>("Playout does not exist.");

        private async Task<Validation<BaseError, Channel>> ChannelMustExist(UpdatePlayout createPlayout) =>
            (await _channelRepository.Get(createPlayout.ChannelId))
            .ToValidation<BaseError>("Channel does not exist.");

        private async Task<Validation<BaseError, ProgramSchedule>>
            ProgramScheduleMustExist(UpdatePlayout createPlayout) =>
            (await _programScheduleRepository.Get(createPlayout.ProgramScheduleId))
            .ToValidation<BaseError>("ProgramSchedule does not exist.");

        private Validation<BaseError, ProgramSchedulePlayoutType> ValidatePlayoutType(UpdatePlayout createPlayout) =>
            Optional(createPlayout.ProgramSchedulePlayoutType)
                .Filter(playoutType => playoutType != ProgramSchedulePlayoutType.None)
                .ToValidation<BaseError>("[ProgramSchedulePlayoutType] must not be None");
    }
}
