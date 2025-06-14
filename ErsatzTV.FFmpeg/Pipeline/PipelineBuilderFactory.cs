using ErsatzTV.FFmpeg.Capabilities;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Pipeline;

public class PipelineBuilderFactory : IPipelineBuilderFactory
{
    private readonly IHardwareCapabilitiesFactory _hardwareCapabilitiesFactory;
    private readonly ILogger<PipelineBuilderFactory> _logger;

    public PipelineBuilderFactory(
        IHardwareCapabilitiesFactory hardwareCapabilitiesFactory,
        ILogger<PipelineBuilderFactory> logger)
    {
        _hardwareCapabilitiesFactory = hardwareCapabilitiesFactory;
        _logger = logger;
    }

    public async Task<IPipelineBuilder> GetBuilder(
        HardwareAccelerationMode hardwareAccelerationMode,
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        Option<ConcatInputFile> concatInputFile,
        Option<string> vaapiDisplay,
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
            vaapiDisplay,
            vaapiDriver,
            vaapiDevice);

        bool isHdrContent = videoInputFile.Any(vif => vif.VideoStreams.Any(vs => vs.ColorParams.IsHdr));

        return hardwareAccelerationMode switch
        {
            HardwareAccelerationMode.Vaapi when capabilities is not NoHardwareCapabilities => new VaapiPipelineBuilder(
                ffmpegCapabilities,
                capabilities,
                hardwareAccelerationMode,
                videoInputFile,
                audioInputFile,
                watermarkInputFile,
                subtitleInputFile,
                concatInputFile,
                reportsFolder,
                fontsFolder,
                _logger),

            HardwareAccelerationMode.Nvenc when capabilities is not NoHardwareCapabilities => new NvidiaPipelineBuilder(
                ffmpegCapabilities,
                capabilities,
                hardwareAccelerationMode,
                videoInputFile,
                audioInputFile,
                watermarkInputFile,
                subtitleInputFile,
                concatInputFile,
                reportsFolder,
                fontsFolder,
                _logger),

            HardwareAccelerationMode.Qsv when capabilities is not NoHardwareCapabilities => new QsvPipelineBuilder(
                ffmpegCapabilities,
                capabilities,
                hardwareAccelerationMode,
                videoInputFile,
                audioInputFile,
                watermarkInputFile,
                subtitleInputFile,
                concatInputFile,
                reportsFolder,
                fontsFolder,
                _logger),

            // force software pipeline when content is HDR (and not VAAPI or NVENC or QSV)
            _ when isHdrContent => new SoftwarePipelineBuilder(
                ffmpegCapabilities,
                HardwareAccelerationMode.None,
                videoInputFile,
                audioInputFile,
                watermarkInputFile,
                subtitleInputFile,
                concatInputFile,
                reportsFolder,
                fontsFolder,
                _logger),

            HardwareAccelerationMode.VideoToolbox when capabilities is not NoHardwareCapabilities => new
                VideoToolboxPipelineBuilder(
                    ffmpegCapabilities,
                    capabilities,
                    hardwareAccelerationMode,
                    videoInputFile,
                    audioInputFile,
                    watermarkInputFile,
                    subtitleInputFile,
                    concatInputFile,
                    reportsFolder,
                    fontsFolder,
                    _logger),

            HardwareAccelerationMode.Amf when capabilities is not NoHardwareCapabilities => new AmfPipelineBuilder(
                ffmpegCapabilities,
                capabilities,
                hardwareAccelerationMode,
                videoInputFile,
                audioInputFile,
                watermarkInputFile,
                subtitleInputFile,
                concatInputFile,
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
                concatInputFile,
                reportsFolder,
                fontsFolder,
                _logger)
        };
    }
}
