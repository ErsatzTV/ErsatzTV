using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Channels.Commands
{
    public class DeleteChannelHandler : IRequestHandler<DeleteChannel, Either<BaseError, Task>>
    {
        private readonly IChannelRepository _channelRepository;

        public DeleteChannelHandler(IChannelRepository channelRepository) => _channelRepository = channelRepository;

        public async Task<Either<BaseError, Task>> Handle(DeleteChannel request, CancellationToken cancellationToken) =>
            (await ChannelMustExist(request))
            .Map(DoDeletion)
            .ToEither<Task>();

        private Task DoDeletion(int channelId) => _channelRepository.Delete(channelId);

        private async Task<Validation<BaseError, int>> ChannelMustExist(DeleteChannel deleteChannel) =>
            (await _channelRepository.Get(deleteChannel.ChannelId))
            .ToValidation<BaseError>($"Channel {deleteChannel.ChannelId} does not exist.")
            .Map(c => c.Id);
    }
}
