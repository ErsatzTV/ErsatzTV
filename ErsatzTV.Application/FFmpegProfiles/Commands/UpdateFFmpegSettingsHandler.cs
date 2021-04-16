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
            await Upsert(ConfigElementKey.FFmpegPath, request.Settings.FFmpegPath);
            await Upsert(ConfigElementKey.FFprobePath, request.Settings.FFprobePath);
            await Upsert(ConfigElementKey.FFmpegDefaultProfileId, request.Settings.DefaultFFmpegProfileId.ToString());
            await Upsert(ConfigElementKey.FFmpegSaveReports, request.Settings.SaveReports.ToString());

            if (request.Settings.SaveReports && !Directory.Exists(FileSystemLayout.FFmpegReportsFolder))
            {
                Directory.CreateDirectory(FileSystemLayout.FFmpegReportsFolder);
            }

            await Upsert(ConfigElementKey.FFmpegPreferredLanguageCode, request.Settings.PreferredLanguageCode);

            return Unit.Default;
        }

        private async Task Upsert(ConfigElementKey key, string value)
        {
            Option<ConfigElement> maybeElement = await _configElementRepository.Get(key); 
            await maybeElement.Match(
                ce =>
                {
                    ce.Value = value;
                    return _configElementRepository.Update(ce);
                },
                () =>
                {
                    var ce = new ConfigElement { Key = key.Key, Value = value };
                    return _configElementRepository.Add(ce);
                });
        }
    }
}
