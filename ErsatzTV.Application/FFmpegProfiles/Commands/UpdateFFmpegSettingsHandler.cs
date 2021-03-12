using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public class UpdateFFmpegSettingsHandler : MediatR.IRequestHandler<UpdateFFmpegSettings, Either<BaseError, Unit>>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly ILocalFileSystem _localFileSystem;

        public UpdateFFmpegSettingsHandler(
            IConfigElementRepository configElementRepository,
            ILocalFileSystem localFileSystem)
        {
            _configElementRepository = configElementRepository;
            _localFileSystem = localFileSystem;
        }

        public Task<Either<BaseError, Unit>> Handle(
            UpdateFFmpegSettings request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyUpdate(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Validation<BaseError, Unit>> Validate(UpdateFFmpegSettings request) =>
            (await FFmpegMustExist(request), await FFprobeMustExist(request))
            .Apply((_, _) => Unit.Default);

        private Task<Validation<BaseError, Unit>> FFmpegMustExist(UpdateFFmpegSettings request) =>
            ValidateToolPath(request.Settings.FFmpegPath, "ffmpeg");

        private Task<Validation<BaseError, Unit>> FFprobeMustExist(UpdateFFmpegSettings request) =>
            ValidateToolPath(request.Settings.FFprobePath, "ffprobe");

        private async Task<Validation<BaseError, Unit>> ValidateToolPath(string path, string name)
        {
            if (!_localFileSystem.FileExists(path))
            {
                return BaseError.New($"{name} path does not exist");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var test = new Process
            {
                StartInfo = startInfo
            };

            test.Start();
            string output = await test.StandardOutput.ReadToEndAsync();
            await test.WaitForExitAsync();
            return test.ExitCode == 0 && output.Contains($"{name} version")
                ? Unit.Default
                : BaseError.New($"Unable to verify {name} version");
        }

        private async Task<Unit> ApplyUpdate(UpdateFFmpegSettings request)
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

            return Unit.Default;
        }
    }
}
