using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Runtime;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Pipeline;

public class PipelineBuilderFactory : IPipelineBuilderFactory
{
    private readonly IRuntimeInfo _runtimeInfo;
    private readonly IHardwareCapabilitiesFactory _hardwareCapabilitiesFactory;
    private readonly ILogger<PipelineBuilderFactory> _logger;

    public PipelineBuilderFactory(
        IRuntimeInfo runtimeInfo,
        IHardwareCapabilitiesFactory hardwareCapabilitiesFactory,
        ILogger<PipelineBuilderFactory> logger)
    {
        _runtimeInfo = runtimeInfo;
        _hardwareCapabilitiesFactory = hardwareCapabilitiesFactory;
        _logger = logger;
    }

    public async Task<IPipelineBuilder> GetBuilder(
        HardwareAccelerationMode hardwareAccelerationMode,
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        string reportsFolder,
        string fontsFolder,
        string ffmpegPath) => hardwareAccelerationMode switch
    {
        HardwareAccelerationMode.Nvenc => new NvidiaPipelineBuilder(
            await _hardwareCapabilitiesFactory.GetHardwareCapabilities(ffmpegPath, hardwareAccelerationMode),
            hardwareAccelerationMode,
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            subtitleInputFile,
            reportsFolder,
            fontsFolder,
            _logger),
        HardwareAccelerationMode.Vaapi => new VaapiPipelineBuilder(
            await _hardwareCapabilitiesFactory.GetHardwareCapabilities(ffmpegPath, hardwareAccelerationMode),
            hardwareAccelerationMode,
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            subtitleInputFile,
            reportsFolder,
            fontsFolder,
            _logger),
        HardwareAccelerationMode.Qsv => new QsvPipelineBuilder(
            await _hardwareCapabilitiesFactory.GetHardwareCapabilities(ffmpegPath, hardwareAccelerationMode),
            hardwareAccelerationMode,
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            subtitleInputFile,
            reportsFolder,
            fontsFolder,
            _logger),
        HardwareAccelerationMode.VideoToolbox => new VideoToolboxPipelineBuilder(
            await _hardwareCapabilitiesFactory.GetHardwareCapabilities(ffmpegPath, hardwareAccelerationMode),
            hardwareAccelerationMode,
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            subtitleInputFile,
            reportsFolder,
            fontsFolder,
            _logger),
        _ => new SoftwarePipelineBuilder(
            hardwareAccelerationMode,
            videoInputFile,
            audioInputFile,
            watermarkInputFile,
            subtitleInputFile,
            reportsFolder,
            fontsFolder,
            _logger)
    };
}
