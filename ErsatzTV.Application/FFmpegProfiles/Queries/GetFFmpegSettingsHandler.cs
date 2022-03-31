using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.FFmpegProfiles;

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
        Option<bool> saveReports =
            await _configElementRepository.GetValue<bool>(ConfigElementKey.FFmpegSaveReports);
        Option<string> preferredAudioLanguageCode =
            await _configElementRepository.GetValue<string>(ConfigElementKey.FFmpegPreferredLanguageCode);
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

        var result = new FFmpegSettingsViewModel
        {
            FFmpegPath = await ffmpegPath.IfNoneAsync(string.Empty),
            FFprobePath = await ffprobePath.IfNoneAsync(string.Empty),
            DefaultFFmpegProfileId = await defaultFFmpegProfileId.IfNoneAsync(0),
            SaveReports = await saveReports.IfNoneAsync(false),
            PreferredAudioLanguageCode = await preferredAudioLanguageCode.IfNoneAsync("eng"),
            HlsSegmenterIdleTimeout = await hlsSegmenterIdleTimeout.IfNoneAsync(60),
            WorkAheadSegmenterLimit = await workAheadSegmenterLimit.IfNoneAsync(1),
            InitialSegmentCount = await initialSegmentCount.IfNoneAsync(1),
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
}