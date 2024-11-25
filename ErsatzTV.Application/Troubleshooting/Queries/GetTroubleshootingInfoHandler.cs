using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;
using ErsatzTV.Application.FFmpegProfiles;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Health;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Capabilities.Qsv;
using ErsatzTV.FFmpeg.OutputFormat;
using ErsatzTV.FFmpeg.Runtime;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ErsatzTV.Application.Troubleshooting.Queries;

public class GetTroubleshootingInfoHandler : IRequestHandler<GetTroubleshootingInfo, TroubleshootingInfo>
{
    private readonly IConfigElementRepository _configElementRepository;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IHardwareCapabilitiesFactory _hardwareCapabilitiesFactory;
    private readonly IHealthCheckService _healthCheckService;
    private readonly IMemoryCache _memoryCache;
    private readonly IRuntimeInfo _runtimeInfo;

    public GetTroubleshootingInfoHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IHealthCheckService healthCheckService,
        IHardwareCapabilitiesFactory hardwareCapabilitiesFactory,
        IConfigElementRepository configElementRepository,
        IRuntimeInfo runtimeInfo,
        IMemoryCache memoryCache)
    {
        _dbContextFactory = dbContextFactory;
        _healthCheckService = healthCheckService;
        _hardwareCapabilitiesFactory = hardwareCapabilitiesFactory;
        _configElementRepository = configElementRepository;
        _runtimeInfo = runtimeInfo;
        _memoryCache = memoryCache;
    }

    public async Task<TroubleshootingInfo> Handle(GetTroubleshootingInfo request, CancellationToken cancellationToken)
    {
        List<HealthCheckResult> healthCheckResults = await _healthCheckService.PerformHealthChecks(cancellationToken);

        string version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

        var healthCheckSummaries = healthCheckResults
            .Filter(r => r.Status is HealthCheckStatus.Warning or HealthCheckStatus.Fail)
            .Map(r => new HealthCheckResultSummary(r.Title, r.Message))
            .ToList();

        FFmpegSettingsViewModel ffmpegSettings = await GetFFmpegSettings();

        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Channel> channels = await dbContext.Channels
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var channelFFmpegProfiles = channels
            .Map(c => c.FFmpegProfileId)
            .ToImmutableHashSet();

        List<FFmpegProfile> ffmpegProfiles = await dbContext.FFmpegProfiles
            .AsNoTracking()
            .Include(p => p.Resolution)
            .ToListAsync(cancellationToken);

        var activeFFmpegProfiles = ffmpegProfiles
            .Filter(f => channelFFmpegProfiles.Contains(f.Id))
            .ToList();

        string nvidiaCapabilities = null;
        string qsvCapabilities = null;
        string vaapiCapabilities = null;
        Option<ConfigElement> maybeFFmpegPath =
            await _configElementRepository.GetConfigElement(ConfigElementKey.FFmpegPath);
        if (maybeFFmpegPath.IsNone)
        {
            nvidiaCapabilities = "Unable to locate ffmpeg";
        }
        else
        {
            foreach (ConfigElement ffmpegPath in maybeFFmpegPath)
            {
                nvidiaCapabilities = await _hardwareCapabilitiesFactory.GetNvidiaOutput(ffmpegPath.Value);

                if (!_memoryCache.TryGetValue("ffmpeg.render_devices", out List<string> vaapiDevices))
                {
                    vaapiDevices = ["/dev/dri/renderD128"];
                }

                if (!_memoryCache.TryGetValue("ffmpeg.vaapi_displays", out List<string> vaapiDisplays))
                {
                    vaapiDisplays = ["drm"];
                }

                foreach (string qsvDevice in vaapiDevices)
                {
                    QsvOutput output = await _hardwareCapabilitiesFactory.GetQsvOutput(ffmpegPath.Value, qsvDevice);
                    qsvCapabilities += $"Checking device {qsvDevice}{Environment.NewLine}";
                    qsvCapabilities += $"Exit Code: {output.ExitCode}{Environment.NewLine}{Environment.NewLine}";
                    qsvCapabilities += output.Output;
                    qsvCapabilities += Environment.NewLine + Environment.NewLine;
                }

                if (_runtimeInfo.IsOSPlatform(OSPlatform.Linux))
                {
                    var allDrivers = new List<VaapiDriver>
                        { VaapiDriver.iHD, VaapiDriver.i965, VaapiDriver.RadeonSI, VaapiDriver.Nouveau };

                    foreach (string display in vaapiDisplays)
                    foreach (VaapiDriver activeDriver in allDrivers)
                    foreach (string vaapiDevice in vaapiDevices)
                    {
                        foreach (string output in await _hardwareCapabilitiesFactory.GetVaapiOutput(
                                     display,
                                     Optional(GetDriverName(activeDriver)),
                                     vaapiDevice))
                        {
                            vaapiCapabilities +=
                                $"Checking display [{display}] driver [{activeDriver}] device [{vaapiDevice}]{Environment.NewLine}{Environment.NewLine}";
                            vaapiCapabilities += output;
                            vaapiCapabilities += Environment.NewLine + Environment.NewLine;
                        }
                    }
                }
            }
        }

        return new TroubleshootingInfo(
            version,
            healthCheckSummaries,
            ffmpegSettings,
            activeFFmpegProfiles,
            channels,
            nvidiaCapabilities,
            qsvCapabilities,
            vaapiCapabilities);
    }

    // lifted from GetFFmpegSettingsHandler
    private async Task<FFmpegSettingsViewModel> GetFFmpegSettings()
    {
        Option<string> ffmpegPath = await _configElementRepository.GetValue<string>(ConfigElementKey.FFmpegPath);
        Option<string> ffprobePath = await _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath);
        Option<int> defaultFFmpegProfileId =
            await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegDefaultProfileId);
        Option<bool> saveReports =
            await _configElementRepository.GetValue<bool>(ConfigElementKey.FFmpegSaveReports);
        Option<string> preferredAudioLanguageCode =
            await _configElementRepository.GetValue<string>(ConfigElementKey.FFmpegPreferredLanguageCode);
        Option<bool> useEmbeddedSubtitles =
            await _configElementRepository.GetValue<bool>(ConfigElementKey.FFmpegUseEmbeddedSubtitles);
        Option<bool> extractEmbeddedSubtitles =
            await _configElementRepository.GetValue<bool>(ConfigElementKey.FFmpegExtractEmbeddedSubtitles);
        Option<int> watermark =
            await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegGlobalWatermarkId);
        Option<int> fallbackFiller =
            await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegGlobalFallbackFillerId);
        Option<int> hlsSegmenterIdleTimeout =
            await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegSegmenterTimeout);
        Option<int> workAheadSegmenterLimit =
            await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegWorkAheadSegmenters);
        Option<int> initialSegmentCount =
            await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegInitialSegmentCount);
        Option<OutputFormatKind> outputFormatKind =
            await _configElementRepository.GetValue<OutputFormatKind>(ConfigElementKey.FFmpegHlsDirectOutputFormat);

        var result = new FFmpegSettingsViewModel
        {
            FFmpegPath = await ffmpegPath.IfNoneAsync(string.Empty),
            FFprobePath = await ffprobePath.IfNoneAsync(string.Empty),
            DefaultFFmpegProfileId = await defaultFFmpegProfileId.IfNoneAsync(0),
            SaveReports = await saveReports.IfNoneAsync(false),
            UseEmbeddedSubtitles = await useEmbeddedSubtitles.IfNoneAsync(true),
            ExtractEmbeddedSubtitles = await extractEmbeddedSubtitles.IfNoneAsync(false),
            PreferredAudioLanguageCode = await preferredAudioLanguageCode.IfNoneAsync("eng"),
            HlsSegmenterIdleTimeout = await hlsSegmenterIdleTimeout.IfNoneAsync(60),
            WorkAheadSegmenterLimit = await workAheadSegmenterLimit.IfNoneAsync(1),
            InitialSegmentCount = await initialSegmentCount.IfNoneAsync(1),
            HlsDirectOutputFormat = await outputFormatKind.IfNoneAsync(OutputFormatKind.MpegTs)
        };

        foreach (int watermarkId in watermark)
        {
            result.GlobalWatermarkId = watermarkId;
        }

        foreach (int fallbackFillerId in fallbackFiller)
        {
            result.GlobalFallbackFillerId = fallbackFillerId;
        }

        return result;
    }

    private static string GetDriverName(VaapiDriver driver)
    {
        switch (driver)
        {
            case VaapiDriver.i965:
                return "i965";
            case VaapiDriver.iHD:
                return "iHD";
            case VaapiDriver.RadeonSI:
                return "radeonsi";
            case VaapiDriver.Nouveau:
                return "nouveau";
        }

        return null;
    }
}
