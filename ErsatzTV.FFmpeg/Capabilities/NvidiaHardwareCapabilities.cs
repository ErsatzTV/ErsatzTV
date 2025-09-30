using ErsatzTV.FFmpeg.Capabilities.Nvidia;
using ErsatzTV.FFmpeg.Format;
using Lennox.NvEncSharp;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Capabilities;

public class NvidiaHardwareCapabilities(CudaDevice cudaDevice, IFFmpegCapabilities ffmpegCapabilities, ILogger logger)
    : IHardwareCapabilities
{
    public bool HevcBFrames(int bitDepth) =>
        cudaDevice.Encoders.Any(e => e.CodecGuid == NvEncCodecGuids.Hevc && e.BFrames);

    public FFmpegCapability CanDecode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat,
        bool isHdr)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        logger.LogDebug(
            "Checking NVIDIA decode {Format} / {Profile} / {BitDepth}-bit",
            videoFormat,
            videoProfile,
            bitDepth);

        var isHardware = false;

        CuVideoCodec? codecType = videoFormat switch
        {
            VideoFormat.Mpeg2Video => CuVideoCodec.MPEG2,
            VideoFormat.Mpeg4 => CuVideoCodec.MPEG4,
            VideoFormat.Vc1 => CuVideoCodec.VC1,
            VideoFormat.H264 => CuVideoCodec.H264,
            VideoFormat.Hevc => CuVideoCodec.HEVC,
            VideoFormat.Vp8 => CuVideoCodec.VP8,
            VideoFormat.Vp9 => CuVideoCodec.VP9,
            VideoFormat.Av1 => (CuVideoCodec)11, // confirmed in dynlink_cuviddec.h
            _ => null
        };

        if (codecType.HasValue)
        {
            isHardware = cudaDevice.Decoders.Any(d => d.VideoCodec == codecType.Value && d.BitDepth == bitDepth);
            if (!isHardware)
            {
                logger.LogWarning(
                    "NVIDIA decode {Format} / {BitDepth} is not supported; will use software decode",
                    videoFormat,
                    bitDepth);
            }
        }

        if (isHardware)
        {
            return videoFormat switch
            {
                VideoFormat.Mpeg2Video => CheckHardwareCodec(
                    FFmpegKnownDecoder.Mpeg2Cuvid,
                    ffmpegCapabilities.HasDecoder),
                VideoFormat.Mpeg4 => CheckHardwareCodec(FFmpegKnownDecoder.Mpeg4Cuvid, ffmpegCapabilities.HasDecoder),
                VideoFormat.Vc1 => CheckHardwareCodec(FFmpegKnownDecoder.Vc1Cuvid, ffmpegCapabilities.HasDecoder),
                VideoFormat.H264 => CheckHardwareCodec(FFmpegKnownDecoder.H264Cuvid, ffmpegCapabilities.HasDecoder),
                VideoFormat.Hevc => CheckHardwareCodec(FFmpegKnownDecoder.HevcCuvid, ffmpegCapabilities.HasDecoder),
                VideoFormat.Vp9 => CheckHardwareCodec(FFmpegKnownDecoder.Vp9Cuvid, ffmpegCapabilities.HasDecoder),
                VideoFormat.Av1 => CheckHardwareCodec(FFmpegKnownDecoder.Av1Cuvid, ffmpegCapabilities.HasDecoder),
                _ => FFmpegCapability.Software
            };
        }

        return FFmpegCapability.Software;
    }

    public FFmpegCapability CanEncode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        logger.LogDebug(
            "Checking NVIDIA encode {Format} / {Profile} / {BitDepth}-bit",
            videoFormat,
            videoProfile,
            bitDepth);

        var codec = cudaDevice.Encoders.FirstOrDefault(c => c.Name.Equals(
            videoFormat,
            StringComparison.OrdinalIgnoreCase));

        if (codec == null)
        {
            logger.LogWarning("NVIDIA encode {Format} is not supported; will use software encode", videoFormat);
            return FFmpegCapability.Software;
        }

        var profileGuid = (videoFormat, videoProfile.IfNone(string.Empty), bitDepth) switch
        {
            (VideoFormat.Hevc, _, 8) => NvEncProfileGuids.HevcMain,
            (VideoFormat.Hevc, _, 10) => NvEncProfileGuids.HevcMain10,

            (VideoFormat.H264, _, 10) => NvEncProfileGuids.H264High444,
            (VideoFormat.H264, VideoProfile.High, _) => NvEncProfileGuids.H264High,
            // high10 is for libx264, nvenc needs high444
            (VideoFormat.H264, VideoProfile.High10, _) => NvEncProfileGuids.H264High444,

            (VideoFormat.Av1, _, _) => CudaHelper.Av1ProfileGuid,

            _ => NvEncProfileGuids.H264Main
        };

        if (!codec.ProfileGuids.Contains(profileGuid))
        {
            logger.LogWarning(
                "NVIDIA encode {Format} / {Profile} is not supported; will use software encode",
                videoFormat,
                videoProfile);
            return FFmpegCapability.Software;
        }

        if (!codec.BitDepths.Contains(bitDepth))
        {
            logger.LogWarning(
                "NVIDIA encode {Format} / {Profile} / {BitDepth}-bit is not supported; will use software encode",
                videoFormat,
                videoProfile,
                bitDepth);
            return FFmpegCapability.Software;
        }

        return FFmpegCapability.Hardware;
    }

    public Option<RateControlMode> GetRateControlMode(string videoFormat, Option<IPixelFormat> maybePixelFormat) =>
        Option<RateControlMode>.None;

    private FFmpegCapability CheckHardwareCodec(FFmpegKnownDecoder codec, Func<FFmpegKnownDecoder, bool> check)
    {
        if (check(codec))
        {
            return FFmpegCapability.Hardware;
        }

        logger.LogWarning("FFmpeg does not contain codec {Codec}; will fall back to software codec", codec.Name);
        return FFmpegCapability.Software;
    }
}
