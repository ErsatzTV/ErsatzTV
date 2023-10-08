using ErsatzTV.FFmpeg.Capabilities.Qsv;

namespace ErsatzTV.FFmpeg.Capabilities;

public interface IHardwareCapabilitiesFactory
{
    Task<IFFmpegCapabilities> GetFFmpegCapabilities(string ffmpegPath);

    Task<IHardwareCapabilities> GetHardwareCapabilities(
        IFFmpegCapabilities ffmpegCapabilities,
        string ffmpegPath,
        HardwareAccelerationMode hardwareAccelerationMode,
        Option<string> vaapiDriver,
        Option<string> vaapiDevice);

    Task<string> GetNvidiaOutput(string ffmpegPath);

    Task<QsvOutput> GetQsvOutput(string ffmpegPath, Option<string> qsvDevice);

    Task<Option<string>> GetVaapiOutput(Option<string> vaapiDriver, string vaapiDevice);
}
