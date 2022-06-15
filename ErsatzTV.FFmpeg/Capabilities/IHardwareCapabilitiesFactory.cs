namespace ErsatzTV.FFmpeg.Capabilities;

public interface IHardwareCapabilitiesFactory
{
    Task<IHardwareCapabilities> GetHardwareCapabilities(
        string ffmpegPath,
        HardwareAccelerationMode hardwareAccelerationMode);
}
