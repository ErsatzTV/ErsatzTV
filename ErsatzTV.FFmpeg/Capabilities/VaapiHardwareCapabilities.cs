using ErsatzTV.FFmpeg.Capabilities.Vaapi;
using ErsatzTV.FFmpeg.Format;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Capabilities;

public class VaapiHardwareCapabilities(
    List<VaapiProfileEntrypoint> profileEntrypoints,
    string generation,
    ILogger logger)
    : IHardwareCapabilities
{
    public int EntrypointCount => profileEntrypoints.Count;

    public FFmpegCapability CanDecode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat,
        bool isHdr)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        bool isPolaris = generation.Contains("polaris", StringComparison.OrdinalIgnoreCase);

        bool isHardware = (videoFormat, videoProfile.IfNone(string.Empty).ToLowerInvariant()) switch
        {
            // no hardware decoding of 10-bit h264
            (VideoFormat.H264, _) when bitDepth == 10 => false,

            // skip polaris hardware decoding 10-bit
            (_, _) when bitDepth == 10 && isPolaris => false,

            // no hardware decoding of h264 baseline profile
            (VideoFormat.H264, "baseline" or "66") => false,

            (VideoFormat.H264, "main" or "77") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.H264Main,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.H264, "high" or "100") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.H264High,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.H264, "high 10" or "110") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.H264High,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.H264, "baseline constrained" or "constrained baseline" or "578") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.H264ConstrainedBaseline,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.Mpeg2Video, "main" or "4") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.Mpeg2Main,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.Mpeg2Video, "simple" or "5") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.Mpeg2Simple,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.Vc1, "simple" or "0") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.Vc1Simple,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.Vc1, "main" or "1") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.Vc1Main,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.Vc1, "advanced" or "3") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.Vc1Advanced,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.Hevc, "main" or "1") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.HevcMain,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.Hevc, "main 10" or "2") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.HevcMain10,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.Vp9, "profile 0" or "0") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.Vp9Profile0,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.Vp9, "profile 1" or "1") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.Vp9Profile1,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.Vp9, "profile 2" or "2") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.Vp9Profile2,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.Vp9, "profile 3" or "3") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.Vp9Profile3,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            (VideoFormat.Av1, "main" or "0") =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.Av1Profile0,
                    VaapiEntrypoint: VaapiEntrypoint.Decode
                }),

            // fall back to software decoder
            _ => false
        };

        if (!isHardware)
        {
            logger.LogDebug(
                "VAAPI does not support decoding {Format}/{Profile}, will use software decoder",
                videoFormat,
                videoProfile);
        }

        return isHardware ? FFmpegCapability.Hardware : FFmpegCapability.Software;
    }

    public FFmpegCapability CanEncode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        bool isHardware = videoFormat switch
        {
            // vaapi cannot encode 10-bit h264
            VideoFormat.H264 when bitDepth == 10 => false,

            VideoFormat.H264 =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.H264Main,
                    VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                }),

            VideoFormat.Hevc when bitDepth == 10 =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.HevcMain10,
                    VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                }),

            VideoFormat.Hevc =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.HevcMain,
                    VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                }),

            VideoFormat.Av1 =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.Av1Profile0,
                    VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                }),

            VideoFormat.Mpeg2Video =>
                profileEntrypoints.Any(e => e is
                {
                    VaapiProfile: VaapiProfile.Mpeg2Main,
                    VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                }),

            _ => false
        };

        if (!isHardware)
        {
            logger.LogDebug(
                "VAAPI does not support encoding {Format} with bit depth {BitDepth}, will use software encoder",
                videoFormat,
                bitDepth);
        }

        return isHardware ? FFmpegCapability.Hardware : FFmpegCapability.Software;
    }

    public Option<RateControlMode> GetRateControlMode(string videoFormat, Option<IPixelFormat> maybePixelFormat)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);
        Option<VaapiProfileEntrypoint> maybeEntrypoint = videoFormat switch
        {
            // vaapi cannot encode 10-bit h264
            VideoFormat.H264 when bitDepth == 10 => None,

            VideoFormat.H264 =>
                profileEntrypoints.Where(e => e is
                    {
                        VaapiProfile: VaapiProfile.H264Main,
                        VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                    })
                    .HeadOrNone(),

            VideoFormat.Hevc when bitDepth == 10 =>
                profileEntrypoints.Where(e => e is
                    {
                        VaapiProfile: VaapiProfile.HevcMain10,
                        VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                    })
                    .HeadOrNone(),

            VideoFormat.Hevc =>
                profileEntrypoints.Where(e => e is
                    {
                        VaapiProfile: VaapiProfile.HevcMain,
                        VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                    })
                    .HeadOrNone(),

            VideoFormat.Mpeg2Video =>
                profileEntrypoints.Where(e => e is
                    {
                        VaapiProfile: VaapiProfile.Mpeg2Main,
                        VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                    })
                    .HeadOrNone(),

            _ => None
        };

        foreach (VaapiProfileEntrypoint entrypoint in maybeEntrypoint)
        {
            if (entrypoint.RateControlModes.Contains(RateControlMode.VBR) ||
                entrypoint.RateControlModes.Contains(RateControlMode.CBR))
            {
                return Option<RateControlMode>.None;
            }

            if (entrypoint.RateControlModes.Contains(RateControlMode.CQP))
            {
                return RateControlMode.CQP;
            }
        }

        return Option<RateControlMode>.None;
    }
}
