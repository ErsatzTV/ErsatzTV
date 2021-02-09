using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Unit = MediatR.Unit;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public class UpdateFFmpegSettingsHandler : IRequestHandler<UpdateFFmpegSettings>
    {
        private readonly IConfigElementRepository _configElementRepository;

        public UpdateFFmpegSettingsHandler(IConfigElementRepository configElementRepository) =>
            _configElementRepository = configElementRepository;

        public async Task<Unit> Handle(UpdateFFmpegSettings request, CancellationToken cancellationToken)
        {
            Option<ConfigElement> ffmpegPath = await _configElementRepository.Get(ConfigElementKey.FFmpegPath);
            Option<ConfigElement> ffprobePath = await _configElementRepository.Get(ConfigElementKey.FFprobePath);
            Option<ConfigElement> defaultFFmpegProfileId =
                await _configElementRepository.Get(ConfigElementKey.FFmpegDefaultProfileId);

            ffmpegPath.Match(
                ce =>
                {
                    ce.Value = request.Settings.FFmpegPath;
                    _configElementRepository.Update(ce);
                },
                () =>
                {
                    var ce = new ConfigElement
                        { Key = ConfigElementKey.FFmpegPath.Key, Value = request.Settings.FFmpegPath };
                    _configElementRepository.Add(ce);
                });

            ffprobePath.Match(
                ce =>
                {
                    ce.Value = request.Settings.FFprobePath;
                    _configElementRepository.Update(ce);
                },
                () =>
                {
                    var ce = new ConfigElement
                        { Key = ConfigElementKey.FFprobePath.Key, Value = request.Settings.FFprobePath };
                    _configElementRepository.Add(ce);
                });

            defaultFFmpegProfileId.Match(
                ce =>
                {
                    ce.Value = request.Settings.DefaultFFmpegProfileId.ToString();
                    _configElementRepository.Update(ce);
                },
                () =>
                {
                    var ce = new ConfigElement
                    {
                        Key = ConfigElementKey.FFmpegDefaultProfileId.Key,
                        Value = request.Settings.DefaultFFmpegProfileId.ToString()
                    };
                    _configElementRepository.Add(ce);
                });

            return Unit.Value;
        }
    }
}
