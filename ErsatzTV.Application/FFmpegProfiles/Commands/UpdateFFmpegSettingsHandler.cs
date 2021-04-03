using System.Diagnostics;
using System.IO;
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
            await _configElementRepository.Get(ConfigElementKey.FFmpegPath).Match(
                async ce =>
                {
                    ce.Value = request.Settings.FFmpegPath;
                    await _configElementRepository.Update(ce);
                },
                async () =>
                {
                    var ce = new ConfigElement
                        { Key = ConfigElementKey.FFmpegPath.Key, Value = request.Settings.FFmpegPath };
                    await _configElementRepository.Add(ce);
                });

            await _configElementRepository.Get(ConfigElementKey.FFprobePath).Match(
                async ce =>
                {
                    ce.Value = request.Settings.FFprobePath;
                    await _configElementRepository.Update(ce);
                },
                async () =>
                {
                    var ce = new ConfigElement
                        { Key = ConfigElementKey.FFprobePath.Key, Value = request.Settings.FFprobePath };
                    await _configElementRepository.Add(ce);
                });

            await _configElementRepository.Get(ConfigElementKey.FFmpegDefaultProfileId).Match(
                async ce =>
                {
                    ce.Value = request.Settings.DefaultFFmpegProfileId.ToString();
                    await _configElementRepository.Update(ce);
                },
                async () =>
                {
                    var ce = new ConfigElement
                    {
                        Key = ConfigElementKey.FFmpegDefaultProfileId.Key,
                        Value = request.Settings.DefaultFFmpegProfileId.ToString()
                    };
                    await _configElementRepository.Add(ce);
                });

            await _configElementRepository.Get(ConfigElementKey.FFmpegSaveReports).Match(
                async ce =>
                {
                    ce.Value = request.Settings.SaveReports.ToString();
                    await _configElementRepository.Update(ce);
                },
                async () =>
                {
                    var ce = new ConfigElement
                    {
                        Key = ConfigElementKey.FFmpegSaveReports.Key,
                        Value = request.Settings.SaveReports.ToString()
                    };
                    await _configElementRepository.Add(ce);
                });

            if (request.Settings.SaveReports && !Directory.Exists(FileSystemLayout.FFmpegReportsFolder))
            {
                Directory.CreateDirectory(FileSystemLayout.FFmpegReportsFolder);
            }

            await _configElementRepository.Get(ConfigElementKey.FFmpegPreferredLanguageCode).Match(
                async ce =>
                {
                    ce.Value = request.Settings.PreferredLanguageCode;
                    await _configElementRepository.Update(ce);
                },
                async () =>
                {
                    var ce = new ConfigElement
                    {
                        Key = ConfigElementKey.FFmpegPreferredLanguageCode.Key,
                        Value = request.Settings.PreferredLanguageCode
                    };
                    await _configElementRepository.Add(ce);
                });


            return Unit.Default;
        }
    }
}
