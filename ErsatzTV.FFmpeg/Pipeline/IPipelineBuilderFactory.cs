namespace ErsatzTV.FFmpeg.Pipeline;

public interface IPipelineBuilderFactory
{
    Task<IPipelineBuilder> GetBuilder(
        HardwareAccelerationMode hardwareAccelerationMode,
        Option<VideoInputFile> videoInputFile,
        Option<AudioInputFile> audioInputFile,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        Option<string> vaapiDriver,
        Option<string> vaapiDevice,
        string reportsFolder,
        string fontsFolder,
        string ffmpegPath);
}
