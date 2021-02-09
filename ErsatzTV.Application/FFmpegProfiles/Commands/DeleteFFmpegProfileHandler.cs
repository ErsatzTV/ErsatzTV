using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public class DeleteFFmpegProfileHandler : IRequestHandler<DeleteFFmpegProfile, Either<BaseError, Task>>
    {
        private readonly IFFmpegProfileRepository _ffmpegProfileRepository;

        public DeleteFFmpegProfileHandler(IFFmpegProfileRepository ffmpegProfileRepository) =>
            _ffmpegProfileRepository = ffmpegProfileRepository;

        public async Task<Either<BaseError, Task>> Handle(
            DeleteFFmpegProfile request,
            CancellationToken cancellationToken) =>
            (await FFmpegProfileMustExist(request))
            .Map(DoDeletion)
            .ToEither<Task>();

        private Task DoDeletion(int channelId) => _ffmpegProfileRepository.Delete(channelId);

        private async Task<Validation<BaseError, int>> FFmpegProfileMustExist(
            DeleteFFmpegProfile deleteFFmpegProfile) =>
            (await _ffmpegProfileRepository.Get(deleteFFmpegProfile.FFmpegProfileId))
            .ToValidation<BaseError>($"FFmpegProfile {deleteFFmpegProfile.FFmpegProfileId} does not exist.")
            .Map(c => c.Id);
    }
}
