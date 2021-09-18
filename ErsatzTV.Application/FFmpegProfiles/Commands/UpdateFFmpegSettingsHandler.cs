using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Runtime;
using LanguageExt;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public class UpdateFFmpegSettingsHandler : MediatR.IRequestHandler<UpdateFFmpegSettings, Either<BaseError, Unit>>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly ILocalFileSystem _localFileSystem;
        private readonly IRuntimeInfo _runtimeInfo;

        public UpdateFFmpegSettingsHandler(
            IConfigElementRepository configElementRepository,
            ILocalFileSystem localFileSystem,
            IRuntimeInfo runtimeInfo)
        {
            _configElementRepository = configElementRepository;
            _localFileSystem = localFileSystem;
            _runtimeInfo = runtimeInfo;
        }

        public Task<Either<BaseError, Unit>> Handle(
            UpdateFFmpegSettings request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyUpdate(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Validation<BaseError, Unit>> Validate(UpdateFFmpegSettings request) =>
            (await FFmpegMustExist(request), await FFprobeMustExist(request), ReportsAreNotSupportedOnWindows(request))
            .Apply((_, _, _) => Unit.Default);

        private Task<Validation<BaseError, Unit>> FFmpegMustExist(UpdateFFmpegSettings request) =>
            ValidateToolPath(request.Settings.FFmpegPath, "ffmpeg");

        private Task<Validation<BaseError, Unit>> FFprobeMustExist(UpdateFFmpegSettings request) =>
            ValidateToolPath(request.Settings.FFprobePath, "ffprobe");

        private Validation<BaseError, Unit> ReportsAreNotSupportedOnWindows(UpdateFFmpegSettings request)
        {
            if (request.Settings.SaveReports && _runtimeInfo.IsOSPlatform(OSPlatform.Windows))
            {
                return BaseError.New("FFmpeg reports are not supported on Windows");
            }

            return Unit.Default;
        }

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
            await _configElementRepository.Upsert(ConfigElementKey.FFmpegPath, request.Settings.FFmpegPath);
            await _configElementRepository.Upsert(ConfigElementKey.FFprobePath, request.Settings.FFprobePath);
            await _configElementRepository.Upsert(
                ConfigElementKey.FFmpegDefaultProfileId,
                request.Settings.DefaultFFmpegProfileId.ToString());
            await _configElementRepository.Upsert(
                ConfigElementKey.FFmpegSaveReports,
                request.Settings.SaveReports.ToString());

            if (request.Settings.SaveReports && !Directory.Exists(FileSystemLayout.FFmpegReportsFolder))
            {
                Directory.CreateDirectory(FileSystemLayout.FFmpegReportsFolder);
            }

            await _configElementRepository.Upsert(
                ConfigElementKey.FFmpegPreferredLanguageCode,
                request.Settings.PreferredLanguageCode);

            if (request.Settings.GlobalWatermarkId is not null)
            {
                await _configElementRepository.Upsert(
                    ConfigElementKey.FFmpegGlobalWatermarkId,
                    request.Settings.GlobalWatermarkId.Value);
            }
            else
            {
                await _configElementRepository.Delete(ConfigElementKey.FFmpegGlobalWatermarkId);
            }

            await _configElementRepository.Upsert(
                ConfigElementKey.FFmpegVaapiDriver,
                (int)request.Settings.VaapiDriver);

            return Unit.Default;
        }
    }
}
