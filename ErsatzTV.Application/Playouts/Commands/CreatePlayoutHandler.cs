using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Playouts.Mapper;
using static LanguageExt.Prelude;
using Channel = ErsatzTV.Core.Domain.Channel;

namespace ErsatzTV.Application.Playouts.Commands
{
    public class
        CreatePlayoutHandler : IRequestHandler<CreatePlayout, Either<BaseError, PlayoutViewModel>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IChannelRepository _channelRepository;
        private readonly IPlayoutRepository _playoutRepository;
        private readonly IProgramScheduleRepository _programScheduleRepository;

        public CreatePlayoutHandler(
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
            CreatePlayout request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(PersistPlayout)
                .Bind(v => v.ToEitherAsync());

        private async Task<PlayoutViewModel> PersistPlayout(Playout c)
        {
            PlayoutViewModel result = await _playoutRepository.Add(c).Map(ProjectToViewModel);
            await _channel.WriteAsync(new BuildPlayout(result.Id));
            return result;
        }

        private async Task<Validation<BaseError, Playout>> Validate(CreatePlayout request) =>
            (await ChannelMustExist(request), await ProgramScheduleMustExist(request), ValidatePlayoutType(request))
            .Apply(
                (channel, programSchedule, playoutType) => new Playout
                {
                    ChannelId = channel.Id,
                    ProgramScheduleId = programSchedule.Id,
                    ProgramSchedulePlayoutType = playoutType
                });

        private async Task<Validation<BaseError, Channel>> ChannelMustExist(CreatePlayout createPlayout) =>
            (await _channelRepository.Get(createPlayout.ChannelId))
            .ToValidation<BaseError>("Channel does not exist.");

        private async Task<Validation<BaseError, ProgramSchedule>> ProgramScheduleMustExist(
            CreatePlayout createPlayout) =>
            (await _programScheduleRepository.GetWithPlayouts(createPlayout.ProgramScheduleId))
            .ToValidation<BaseError>("ProgramSchedule does not exist.")
            .Bind(ProgramScheduleMustHaveItems);

        private Validation<BaseError, ProgramSchedule> ProgramScheduleMustHaveItems(ProgramSchedule programSchedule) =>
            Optional(programSchedule)
                .Filter(ps => ps.Items.Any())
                .ToValidation<BaseError>("Program schedule must have items");

        private Validation<BaseError, ProgramSchedulePlayoutType> ValidatePlayoutType(CreatePlayout createPlayout) =>
            Optional(createPlayout.ProgramSchedulePlayoutType)
                .Filter(playoutType => playoutType != ProgramSchedulePlayoutType.None)
                .ToValidation<BaseError>("[ProgramSchedulePlayoutType] must not be None");
    }
}
