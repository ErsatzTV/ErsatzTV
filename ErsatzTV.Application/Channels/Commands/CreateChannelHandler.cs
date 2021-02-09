using System;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels.Commands
{
    public class CreateChannelHandler : IRequestHandler<CreateChannel, Either<BaseError, ChannelViewModel>>
    {
        private readonly IChannelRepository _channelRepository;
        private readonly IFFmpegProfileRepository _ffmpegProfileRepository;

        public CreateChannelHandler(
            IChannelRepository channelRepository,
            IFFmpegProfileRepository ffmpegProfileRepository)
        {
            _channelRepository = channelRepository;
            _ffmpegProfileRepository = ffmpegProfileRepository;
        }

        public Task<Either<BaseError, ChannelViewModel>> Handle(
            CreateChannel request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(PersistChannel)
                .Bind(v => v.ToEitherAsync());

        private Task<ChannelViewModel> PersistChannel(Channel c) =>
            _channelRepository.Add(c).Map(ProjectToViewModel);

        private async Task<Validation<BaseError, Channel>> Validate(CreateChannel request) =>
            (ValidateName(request), ValidateNumber(request), await FFmpegProfileMustExist(request))
            .Apply(
                (name, number, ffmpegProfileId) => new Channel(Guid.NewGuid())
                {
                    Name = name, Number = number, FFmpegProfileId = ffmpegProfileId,
                    StreamingMode = request.StreamingMode
                });

        private Validation<BaseError, string> ValidateName(CreateChannel createChannel) =>
            createChannel.NotEmpty(c => c.Name)
                .Bind(_ => createChannel.NotLongerThan(50)(c => c.Name));

        // TODO: validate number does not exist?
        private Validation<BaseError, int> ValidateNumber(CreateChannel createChannel) =>
            createChannel.AtLeast(1)(c => c.Number);

        private async Task<Validation<BaseError, int>> FFmpegProfileMustExist(CreateChannel createChannel) =>
            (await _ffmpegProfileRepository.Get(createChannel.FFmpegProfileId))
            .ToValidation<BaseError>($"FFmpegProfile {createChannel.FFmpegProfileId} does not exist.")
            .Map(c => c.Id);
    }
}
