using ErsatzTV.FFmpeg.Capabilities.Nvidia;
using ErsatzTV.FFmpeg.Format;
using Lennox.NvEncSharp;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Capabilities;

public class NvidiaHardwareCapabilities : IHardwareCapabilities
{
    private readonly CudaDevice _cudaDevice;
    private readonly IFFmpegCapabilities _ffmpegCapabilities;
    private readonly ILogger _logger;
    private readonly List<string> _maxwellGm206 = ["GTX 750", "GTX 950", "GTX 960", "GTX 965M"];
    private readonly Version _maxwell = new(5, 2);
    private readonly Version _pascal = new(6, 0);
    private readonly Version _ampere = new(8, 6);

    public NvidiaHardwareCapabilities(
        CudaDevice cudaDevice,
        IFFmpegCapabilities ffmpegCapabilities,
        ILogger logger)
    {
        _cudaDevice = cudaDevice;
        _ffmpegCapabilities = ffmpegCapabilities;
        _logger = logger;
    }

    // this fails with some 1650 cards, so let's try greater than 75
    public bool HevcBFrames => _cudaDevice.Version >= new Version(7, 5);

    public FFmpegCapability CanDecode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat,
        bool isHdr)
    {
        // we use vulkan for hdr, so only support h264, hevc and av1 when isHdr == true

        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        bool isHardware = videoFormat switch
        {
            // some second gen maxwell can decode hevc, otherwise pascal is required
            VideoFormat.Hevc => _cudaDevice.Version == _maxwell && _maxwellGm206.Contains(_cudaDevice.Model) || _cudaDevice.Version >= _pascal,

            // pascal is required to decode vp9 10-bit
            VideoFormat.Vp9 when bitDepth == 10 => !isHdr && _cudaDevice.Version >= _pascal,

            // some second gen maxwell can decode vp9, otherwise pascal is required
            VideoFormat.Vp9 => !isHdr && _cudaDevice.Version == _maxwell && _maxwellGm206.Contains(_cudaDevice.Model) || _cudaDevice.Version >= _pascal,

            // no hardware decoding of 10-bit h264
            VideoFormat.H264 => bitDepth < 10,

            VideoFormat.Mpeg2Video => !isHdr,

            VideoFormat.Vc1 => !isHdr,

            // too many issues with odd mpeg4 content, so use software
            VideoFormat.Mpeg4 => false,

            // ampere is required for av1 decoding
            VideoFormat.Av1 => _cudaDevice.Version >= _ampere,

            // generated images are decoded into software
            VideoFormat.GeneratedImage => false,

            _ => false
        };

        if (isHardware)
        {
            return videoFormat switch
            {
                VideoFormat.Mpeg2Video => CheckHardwareCodec(
                    FFmpegKnownDecoder.Mpeg2Cuvid,
                    _ffmpegCapabilities.HasDecoder),
                VideoFormat.Mpeg4 => CheckHardwareCodec(FFmpegKnownDecoder.Mpeg4Cuvid, _ffmpegCapabilities.HasDecoder),
                VideoFormat.Vc1 => CheckHardwareCodec(FFmpegKnownDecoder.Vc1Cuvid, _ffmpegCapabilities.HasDecoder),
                VideoFormat.H264 => CheckHardwareCodec(FFmpegKnownDecoder.H264Cuvid, _ffmpegCapabilities.HasDecoder),
                VideoFormat.Hevc => CheckHardwareCodec(FFmpegKnownDecoder.HevcCuvid, _ffmpegCapabilities.HasDecoder),
                VideoFormat.Vp9 => CheckHardwareCodec(FFmpegKnownDecoder.Vp9Cuvid, _ffmpegCapabilities.HasDecoder),
                VideoFormat.Av1 => CheckHardwareCodec(FFmpegKnownDecoder.Av1Cuvid, _ffmpegCapabilities.HasDecoder),
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

        try
        {
            var dev = CuDevice.GetDevice(0);
            using var context = dev.CreateContext();
            var sessionParams = new NvEncOpenEncodeSessionExParams
            {
                Version = LibNvEnc.NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS_VER,
                ApiVersion = LibNvEnc.NVENCAPI_VERSION,
                Device = context.Handle,
                DeviceType = NvEncDeviceType.Cuda
            };

            var encoder = LibNvEnc.OpenEncoder(ref sessionParams);
            try
            {
                _logger.LogDebug(
                    "Checking NvEnc {Format} / {Profile} / {BitDepth}-bit",
                    videoFormat,
                    videoProfile,
                    bitDepth);

                var codecGuid = videoFormat switch
                {
                    VideoFormat.Hevc => NvEncCodecGuids.Hevc,
                    _ => NvEncCodecGuids.H264
                };

                IReadOnlyList<Guid> codecGuids = encoder.GetEncodeGuids();
                if (!codecGuids.Contains(codecGuid))
                {
                    _logger.LogWarning("NvEnc {Format} is not supported; will use software encode", videoFormat);
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

                    _ => NvEncProfileGuids.H264Main
                };

                IReadOnlyList<Guid> profileGuids = encoder.GetEncodeProfileGuids(codecGuid);
                if (!profileGuids.Contains(profileGuid))
                {
                    _logger.LogWarning(
                        "NvEnc {Format} / {Profile} is not supported; will use software encode",
                        videoFormat,
                        videoProfile);
                    return FFmpegCapability.Software;
                }

                if (bitDepth == 10)
                {
                    var cap = new NvEncCapsParam { CapsToQuery = NvEncCaps.Support10bitEncode };
                    var capsVal = 0;
                    encoder.GetEncodeCaps(codecGuid, ref cap, ref capsVal);
                    if (capsVal == 0)
                    {
                        _logger.LogWarning(
                            "NvEnc {Format} / {Profile} / {BitDepth}-bit is not supported; will use software encode",
                            videoFormat,
                            videoProfile,
                            bitDepth);
                        return FFmpegCapability.Software;
                    }
                }

                return FFmpegCapability.Hardware;
            }
            finally
            {
                encoder.DestroyEncoder();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error checking NvEnc capabilities; falling back to software");
        }

        return FFmpegCapability.Software;
    }

    public Option<RateControlMode> GetRateControlMode(string videoFormat, Option<IPixelFormat> maybePixelFormat) =>
        Option<RateControlMode>.None;

    private FFmpegCapability CheckHardwareCodec(FFmpegKnownDecoder codec, Func<FFmpegKnownDecoder, bool> check)
    {
        if (check(codec))
        {
            return FFmpegCapability.Hardware;
        }

        _logger.LogWarning("FFmpeg does not contain codec {Codec}; will fall back to software codec", codec.Name);
        return FFmpegCapability.Software;
    }
}
