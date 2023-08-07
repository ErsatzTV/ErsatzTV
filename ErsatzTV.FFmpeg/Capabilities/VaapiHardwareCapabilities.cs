using ErsatzTV.FFmpeg.Capabilities.Vaapi;
using ErsatzTV.FFmpeg.Format;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Capabilities;

public class VaapiHardwareCapabilities : IHardwareCapabilities
{
    private readonly ILogger _logger;
    private readonly List<VaapiProfileEntrypoint> _profileEntrypoints;

    public VaapiHardwareCapabilities(List<VaapiProfileEntrypoint> profileEntrypoints, ILogger logger)
    {
        _profileEntrypoints = profileEntrypoints;
        _logger = logger;
    }

    public FFmpegCapability CanDecode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        bool isHardware = (videoFormat, videoProfile.IfNone(string.Empty).ToLowerInvariant()) switch
        {
            // no hardware decoding of 10-bit h264
            (VideoFormat.H264, _) when bitDepth == 10 => false,

            // no hardware decoding of h264 baseline profile
            (VideoFormat.H264, "baseline" or "66") => false,

            (VideoFormat.H264, "main" or "77") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.H264Main,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.H264, "high" or "100") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.H264High,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.H264, "high 10" or "110") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.H264High,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.H264, "baseline constrained" or "578") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.H264ConstrainedBaseline,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.Mpeg2Video, "main" or "4") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.Mpeg2Main,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.Mpeg2Video, "simple" or "5") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.Mpeg2Simple,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.Vc1, "simple" or "0") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.Vc1Simple,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.Vc1, "main" or "1") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.Vc1Main,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.Vc1, "advanced" or "3") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.Vc1Advanced,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.Hevc, "main" or "1") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.HevcMain,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.Hevc, "main 10" or "2") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.HevcMain10,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.Vp9, "profile 0" or "0") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.Vp9Profile0,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.Vp9, "profile 1" or "1") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.Vp9Profile1,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.Vp9, "profile 2" or "2") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.Vp9Profile2,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.Vp9, "profile 3" or "3") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.Vp9Profile3,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            (VideoFormat.Av1, "main" or "0") =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.Av1Profile0,
                        VaapiEntrypoint: VaapiEntrypoint.Decode
                    }),

            // fall back to software decoder
            _ => false
        };

        if (!isHardware)
        {
            _logger.LogDebug(
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
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.H264Main,
                        VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                    }),

            VideoFormat.Hevc when bitDepth == 10 =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.HevcMain10,
                        VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                    }),

            VideoFormat.Hevc =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.HevcMain,
                        VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                    }),

            VideoFormat.Mpeg2Video =>
                _profileEntrypoints.Any(
                    e => e is
                    {
                        VaapiProfile: VaapiProfile.Mpeg2Main,
                        VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                    }),

            _ => false
        };

        if (!isHardware)
        {
            _logger.LogDebug(
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
                _profileEntrypoints.Where(
                        e => e is
                        {
                            VaapiProfile: VaapiProfile.H264Main,
                            VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                        })
                    .HeadOrNone(),

            VideoFormat.Hevc when bitDepth == 10 =>
                _profileEntrypoints.Where(
                        e => e is
                        {
                            VaapiProfile: VaapiProfile.HevcMain10,
                            VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                        })
                    .HeadOrNone(),

            VideoFormat.Hevc =>
                _profileEntrypoints.Where(
                        e => e is
                        {
                            VaapiProfile: VaapiProfile.HevcMain,
                            VaapiEntrypoint: VaapiEntrypoint.Encode or VaapiEntrypoint.EncodeLowPower
                        })
                    .HeadOrNone(),

            VideoFormat.Mpeg2Video =>
                _profileEntrypoints.Where(
                        e => e is
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
