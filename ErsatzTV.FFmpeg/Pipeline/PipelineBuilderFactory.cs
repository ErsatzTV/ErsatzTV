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
        Option<string> vaapiDriver,
        Option<string> vaapiDevice,
        string reportsFolder,
        string fontsFolder,
        string ffmpegPath)
    {
        IFFmpegCapabilities ffmpegCapabilities = await _hardwareCapabilitiesFactory.GetFFmpegCapabilities(ffmpegPath);
        
        IHardwareCapabilities capabilities = await _hardwareCapabilitiesFactory.GetHardwareCapabilities(
            ffmpegCapabilities,
            ffmpegPath,
            hardwareAccelerationMode,
            vaapiDriver,
            vaapiDevice);

        return hardwareAccelerationMode switch
        {
            HardwareAccelerationMode.Nvenc when capabilities is not NoHardwareCapabilities => new NvidiaPipelineBuilder(
                ffmpegCapabilities,
                capabilities,
                hardwareAccelerationMode,
                videoInputFile,
                audioInputFile,
                watermarkInputFile,
                subtitleInputFile,
                reportsFolder,
                fontsFolder,
                _logger),
            HardwareAccelerationMode.Vaapi when capabilities is not NoHardwareCapabilities => new VaapiPipelineBuilder(
                ffmpegCapabilities,
                capabilities,
                hardwareAccelerationMode,
                videoInputFile,
                audioInputFile,
                watermarkInputFile,
                subtitleInputFile,
                reportsFolder,
                fontsFolder,
                _logger),
            HardwareAccelerationMode.Qsv => new QsvPipelineBuilder(
                ffmpegCapabilities,
                capabilities,
                hardwareAccelerationMode,
                videoInputFile,
                audioInputFile,
                watermarkInputFile,
                subtitleInputFile,
                reportsFolder,
                fontsFolder,
                _logger),
            HardwareAccelerationMode.VideoToolbox => new VideoToolboxPipelineBuilder(
                ffmpegCapabilities,
                capabilities,
                hardwareAccelerationMode,
                videoInputFile,
                audioInputFile,
                watermarkInputFile,
                subtitleInputFile,
                reportsFolder,
                fontsFolder,
                _logger),
            HardwareAccelerationMode.Amf => new AmfPipelineBuilder(
                ffmpegCapabilities,
                capabilities,
                hardwareAccelerationMode,
                videoInputFile,
                audioInputFile,
                watermarkInputFile,
                subtitleInputFile,
                reportsFolder,
                fontsFolder,
                _logger),
            _ => new SoftwarePipelineBuilder(
                ffmpegCapabilities,
                HardwareAccelerationMode.None,
                videoInputFile,
                audioInputFile,
                watermarkInputFile,
                subtitleInputFile,
                reportsFolder,
                fontsFolder,
                _logger)
        };
    }
}
