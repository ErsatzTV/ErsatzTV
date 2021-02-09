using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Queries
{
    public class GetFFmpegSettingsHandler : IRequestHandler<GetFFmpegSettings, FFmpegSettingsViewModel>
    {
        private readonly IConfigElementRepository _configElementRepository;

        public GetFFmpegSettingsHandler(IConfigElementRepository configElementRepository) =>
            _configElementRepository = configElementRepository;

        public async Task<FFmpegSettingsViewModel> Handle(
            GetFFmpegSettings request,
            CancellationToken cancellationToken)
        {
            Option<string> ffmpegPath = await _configElementRepository.GetValue<string>(ConfigElementKey.FFmpegPath);
            Option<string> ffprobePath = await _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath);
            Option<int> defaultFFmpegProfileId =
                await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegDefaultProfileId);

            return new FFmpegSettingsViewModel
            {
                FFmpegPath = ffmpegPath.IfNone(string.Empty),
                FFprobePath = ffprobePath.IfNone(string.Empty),
                DefaultFFmpegProfileId = defaultFFmpegProfileId.IfNone(0)
            };
        }
    }
}
