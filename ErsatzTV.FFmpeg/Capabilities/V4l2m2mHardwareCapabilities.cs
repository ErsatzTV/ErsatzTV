using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class V4l2m2mHardwareCapabilities(IFFmpegCapabilities ffmpegCapabilities) : IHardwareCapabilities
{
    public FFmpegCapability CanDecode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat,
        bool isHdr)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        return (videoFormat, bitDepth) switch
        {
            (VideoFormat.H264, 8) => ffmpegCapabilities.HasDecoder(FFmpegKnownDecoder.H264V4l2m2m)
                ? FFmpegCapability.Hardware
                : FFmpegCapability.Software,

            (VideoFormat.Hevc, _) => ffmpegCapabilities.HasDecoder(FFmpegKnownDecoder.HevcV4l2m2m)
                ? FFmpegCapability.Hardware
                : FFmpegCapability.Software,

            (VideoFormat.Mpeg2Video, 8) => ffmpegCapabilities.HasDecoder(FFmpegKnownDecoder.Mpeg2V4l2m2m)
                ? FFmpegCapability.Hardware
                : FFmpegCapability.Software,

            (VideoFormat.Mpeg4, _) => ffmpegCapabilities.HasDecoder(FFmpegKnownDecoder.Mpeg4V4l2m2m)
                ? FFmpegCapability.Hardware
                : FFmpegCapability.Software,

            (VideoFormat.Vc1, _) => ffmpegCapabilities.HasDecoder(FFmpegKnownDecoder.Vc1V4l2m2m)
                ? FFmpegCapability.Hardware
                : FFmpegCapability.Software,

            (VideoFormat.Vp8, _) => ffmpegCapabilities.HasDecoder(FFmpegKnownDecoder.Vp84V4l2m2m)
                ? FFmpegCapability.Hardware
                : FFmpegCapability.Software,

            (VideoFormat.Vp9, _) => ffmpegCapabilities.HasDecoder(FFmpegKnownDecoder.Vp94V4l2m2m)
                ? FFmpegCapability.Hardware
                : FFmpegCapability.Software,

            _ => FFmpegCapability.Software
        };    
    }

    public FFmpegCapability CanEncode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        return (videoFormat, bitDepth) switch
        {
            // 10-bit h264 encoding is not support by any hardware
            (VideoFormat.H264, 10) => FFmpegCapability.Software,

            _ => FFmpegCapability.Hardware
        };
    }

    public Option<RateControlMode> GetRateControlMode(string videoFormat, Option<IPixelFormat> maybePixelFormat) =>
        Option<RateControlMode>.None;
}
