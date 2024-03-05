﻿using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;
using ErsatzTV.Application.Subtitles;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.FFmpegProfiles;

public class UpdateFFmpegSettingsHandler : IRequestHandler<UpdateFFmpegSettings, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;

    public UpdateFFmpegSettingsHandler(
        IConfigElementRepository configElementRepository,
        ILocalFileSystem localFileSystem,
        ChannelWriter<IBackgroundServiceRequest> workerChannel)
    {
        _configElementRepository = configElementRepository;
        _localFileSystem = localFileSystem;
        _workerChannel = workerChannel;
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

        using var test = new Process();
        test.StartInfo = startInfo;

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
            request.Settings.DefaultFFmpegProfileId.ToString(CultureInfo.InvariantCulture));
        await _configElementRepository.Upsert(
            ConfigElementKey.FFmpegSaveReports,
            request.Settings.SaveReports.ToString());
        await _configElementRepository.Upsert(
            ConfigElementKey.FFmpegHlsDirectOutputFormat,
            request.Settings.HlsDirectOutputFormat);

        if (request.Settings.SaveReports && !Directory.Exists(FileSystemLayout.FFmpegReportsFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.FFmpegReportsFolder);
        }

        await _configElementRepository.Upsert(
            ConfigElementKey.FFmpegPreferredLanguageCode,
            request.Settings.PreferredAudioLanguageCode);

        await _configElementRepository.Upsert(
            ConfigElementKey.FFmpegUseEmbeddedSubtitles,
            request.Settings.UseEmbeddedSubtitles);

        // do not extract when subtitles are not used
        if (request.Settings.UseEmbeddedSubtitles == false)
        {
            request.Settings.ExtractEmbeddedSubtitles = false;
        }

        await _configElementRepository.Upsert(
            ConfigElementKey.FFmpegExtractEmbeddedSubtitles,
            request.Settings.ExtractEmbeddedSubtitles);

        // queue extracting all embedded subtitles
        if (request.Settings.ExtractEmbeddedSubtitles)
        {
            await _workerChannel.WriteAsync(new ExtractEmbeddedSubtitles(Option<int>.None));
        }

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

        if (request.Settings.GlobalFallbackFillerId is not null)
        {
            await _configElementRepository.Upsert(
                ConfigElementKey.FFmpegGlobalFallbackFillerId,
                request.Settings.GlobalFallbackFillerId.Value);
        }
        else
        {
            await _configElementRepository.Delete(ConfigElementKey.FFmpegGlobalFallbackFillerId);
        }

        await _configElementRepository.Upsert(
            ConfigElementKey.FFmpegSegmenterTimeout,
            request.Settings.HlsSegmenterIdleTimeout);

        await _configElementRepository.Upsert(
            ConfigElementKey.FFmpegWorkAheadSegmenters,
            request.Settings.WorkAheadSegmenterLimit);

        await _configElementRepository.Upsert(
            ConfigElementKey.FFmpegInitialSegmentCount,
            request.Settings.InitialSegmentCount);

        return Unit.Default;
    }
}
