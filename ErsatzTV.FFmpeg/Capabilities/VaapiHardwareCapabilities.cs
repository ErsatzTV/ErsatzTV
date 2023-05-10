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
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.H264Main, VaapiEntrypoint.Decode)),

            (VideoFormat.H264, "high" or "100") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.H264High, VaapiEntrypoint.Decode)),

            (VideoFormat.H264, "high 10" or "110") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.H264High, VaapiEntrypoint.Decode)),

            (VideoFormat.H264, "baseline constrained" or "578") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.H264ConstrainedBaseline, VaapiEntrypoint.Decode)),

            (VideoFormat.Mpeg2Video, "main" or "4") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.Mpeg2Main, VaapiEntrypoint.Decode)),

            (VideoFormat.Mpeg2Video, "simple" or "5") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.Mpeg2Simple, VaapiEntrypoint.Decode)),

            (VideoFormat.Vc1, "simple" or "0") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.Vc1Simple, VaapiEntrypoint.Decode)),

            (VideoFormat.Vc1, "main" or "1") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.Vc1Main, VaapiEntrypoint.Decode)),

            (VideoFormat.Vc1, "advanced" or "3") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.Vc1Advanced, VaapiEntrypoint.Decode)),

            (VideoFormat.Hevc, "main" or "1") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.HevcMain, VaapiEntrypoint.Decode)),

            (VideoFormat.Hevc, "main 10" or "2") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.HevcMain10, VaapiEntrypoint.Decode)),

            (VideoFormat.Vp9, "profile 0" or "0") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.Vp9Profile0, VaapiEntrypoint.Decode)),

            (VideoFormat.Vp9, "profile 1" or "1") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.Vp9Profile1, VaapiEntrypoint.Decode)),

            (VideoFormat.Vp9, "profile 2" or "2") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.Vp9Profile2, VaapiEntrypoint.Decode)),

            (VideoFormat.Vp9, "profile 3" or "3") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.Vp9Profile3, VaapiEntrypoint.Decode)),

            (VideoFormat.Av1, "main" or "0") =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.Av1Profile0, VaapiEntrypoint.Decode)),

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
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.H264Main, VaapiEntrypoint.Encode)) ||
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.H264Main, VaapiEntrypoint.EncodeLowPower)),

            VideoFormat.Hevc when bitDepth == 10 =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.HevcMain10, VaapiEntrypoint.Encode)) ||
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.HevcMain10, VaapiEntrypoint.EncodeLowPower)),

            VideoFormat.Hevc =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.HevcMain, VaapiEntrypoint.Encode)) ||
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.HevcMain, VaapiEntrypoint.EncodeLowPower)),

            VideoFormat.Mpeg2Video =>
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.Mpeg2Main, VaapiEntrypoint.Encode)) ||
                _profileEntrypoints.Contains(
                    new VaapiProfileEntrypoint(VaapiProfile.Mpeg2Main, VaapiEntrypoint.EncodeLowPower)),

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
}
