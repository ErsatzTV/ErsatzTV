using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.OutputFormat;

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
        Option<string> ffmpegPath = await _configElementRepository.GetValue<string>(ConfigElementKey.FFmpegPath, cancellationToken);
        Option<string> ffprobePath = await _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath, cancellationToken);
        Option<int> defaultFFmpegProfileId =
            await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegDefaultProfileId, cancellationToken);
        Option<bool> saveReports =
            await _configElementRepository.GetValue<bool>(ConfigElementKey.FFmpegSaveReports, cancellationToken);
        Option<string> preferredAudioLanguageCode =
            await _configElementRepository.GetValue<string>(ConfigElementKey.FFmpegPreferredLanguageCode, cancellationToken);
        Option<bool> useEmbeddedSubtitles =
            await _configElementRepository.GetValue<bool>(ConfigElementKey.FFmpegUseEmbeddedSubtitles, cancellationToken);
        Option<bool> extractEmbeddedSubtitles =
            await _configElementRepository.GetValue<bool>(ConfigElementKey.FFmpegExtractEmbeddedSubtitles, cancellationToken);
        Option<int> watermark =
            await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegGlobalWatermarkId, cancellationToken);
        Option<int> fallbackFiller =
            await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegGlobalFallbackFillerId, cancellationToken);
        Option<int> hlsSegmenterIdleTimeout =
            await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegSegmenterTimeout, cancellationToken);
        Option<int> workAheadSegmenterLimit =
            await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegWorkAheadSegmenters, cancellationToken);
        Option<int> initialSegmentCount =
            await _configElementRepository.GetValue<int>(ConfigElementKey.FFmpegInitialSegmentCount, cancellationToken);
        Option<OutputFormatKind> outputFormatKind =
            await _configElementRepository.GetValue<OutputFormatKind>(ConfigElementKey.FFmpegHlsDirectOutputFormat, cancellationToken);

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
}
