using ErsatzTV.FFmpeg.Decoder;

namespace ErsatzTV.FFmpeg.Capabilities;

public interface IFFmpegCapabilities
{
    bool HasHardwareAcceleration(HardwareAccelerationMode hardwareAccelerationMode);
    bool HasDecoder(string decoder);
    bool HasEncoder(string encoder);
    bool HasFilter(string filter);
    Option<IDecoder> SoftwareDecoderForVideoFormat(string videoFormat);
}
