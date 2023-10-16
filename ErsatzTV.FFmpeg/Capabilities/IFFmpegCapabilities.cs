using ErsatzTV.FFmpeg.Decoder;

namespace ErsatzTV.FFmpeg.Capabilities;

public interface IFFmpegCapabilities
{
    bool HasHardwareAcceleration(HardwareAccelerationMode hardwareAccelerationMode);
    bool HasDecoder(FFmpegKnownDecoder decoder);
    bool HasEncoder(FFmpegKnownEncoder encoder);
    bool HasFilter(FFmpegKnownFilter filter);
    bool HasOption(FFmpegKnownOption ffmpegOption);
    Option<IDecoder> SoftwareDecoderForVideoFormat(string videoFormat);
}
