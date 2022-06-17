using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Decoder.Cuvid;
using ErsatzTV.FFmpeg.Decoder.Qsv;
using ErsatzTV.FFmpeg.Format;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Decoder;

public static class AvailableDecoders
{
    public static Option<IDecoder> ForVideoFormat(
        IHardwareCapabilities hardwareCapabilities,
        FFmpegState ffmpegState,
        FrameState currentState,
        FrameState desiredState,
        Option<WatermarkInputFile> watermarkInputFile,
        Option<SubtitleInputFile> subtitleInputFile,
        ILogger logger) =>
        (ffmpegState.DecoderHardwareAccelerationMode, currentState.VideoFormat,
                currentState.PixelFormat.Match(pf => pf.Name, () => string.Empty)) switch
            {
                (HardwareAccelerationMode.Nvenc, VideoFormat.Hevc, _)
                    when hardwareCapabilities.CanDecode(VideoFormat.Hevc, currentState.PixelFormat) =>
                    new DecoderHevcCuvid(ffmpegState),

                // nvenc doesn't support hardware decoding of 10-bit content
                (HardwareAccelerationMode.Nvenc, VideoFormat.H264, PixelFormat.YUV420P10LE or PixelFormat.YUV444P10LE)
                    => new DecoderH264(),

                // mpeg2_cuvid seems to have issues when yadif_cuda is used, so just use software decoding
                (HardwareAccelerationMode.Nvenc, VideoFormat.Mpeg2Video, _) when desiredState.Deinterlaced =>
                    new DecoderMpeg2Video(),

                (HardwareAccelerationMode.Nvenc, VideoFormat.H264, _)
                    when hardwareCapabilities.CanDecode(VideoFormat.H264, currentState.PixelFormat) =>
                    new DecoderH264Cuvid(ffmpegState),
                (HardwareAccelerationMode.Nvenc, VideoFormat.Mpeg2Video, _) => new DecoderMpeg2Cuvid(
                    ffmpegState,
                    desiredState.Deinterlaced),
                (HardwareAccelerationMode.Nvenc, VideoFormat.Vc1, _) => new DecoderVc1Cuvid(ffmpegState),
                (HardwareAccelerationMode.Nvenc, VideoFormat.Vp9, _)
                    when hardwareCapabilities.CanDecode(VideoFormat.Vp9, currentState.PixelFormat) =>
                    new DecoderVp9Cuvid(ffmpegState),
                (HardwareAccelerationMode.Nvenc, VideoFormat.Mpeg4, _) => new DecoderMpeg4Cuvid(ffmpegState),

                // hevc_qsv decoder sometimes causes green lines with 10-bit content
                (HardwareAccelerationMode.Qsv, VideoFormat.Hevc, PixelFormat.YUV420P10LE) => new DecoderHevc(),

                // h264_qsv does not support decoding 10-bit content
                (HardwareAccelerationMode.Qsv, VideoFormat.H264, PixelFormat.YUV420P10LE or PixelFormat.YUV444P10LE) =>
                    new DecoderH264(),

                // qsv uses software deinterlace filter, so decode in software
                (HardwareAccelerationMode.Qsv, VideoFormat.H264, _) when desiredState.Deinterlaced => new DecoderH264(),
                (HardwareAccelerationMode.Qsv, VideoFormat.Mpeg2Video, _) when desiredState.Deinterlaced =>
                    new DecoderMpeg2Video(),

                (HardwareAccelerationMode.Qsv, VideoFormat.Hevc, _) => new DecoderHevcQsv(),
                (HardwareAccelerationMode.Qsv, VideoFormat.H264, _) => new DecoderH264Qsv(),
                (HardwareAccelerationMode.Qsv, VideoFormat.Mpeg2Video, _) => new DecoderMpeg2Qsv(),
                (HardwareAccelerationMode.Qsv, VideoFormat.Vc1, _) => new DecoderVc1Qsv(),
                (HardwareAccelerationMode.Qsv, VideoFormat.Vp9, _) => new DecoderVp9Qsv(),

                // vaapi should use implicit decoders when scaling or no watermark/subtitles
                // otherwise, fall back to software decoders
                (HardwareAccelerationMode.Vaapi, _, _) when watermarkInputFile.IsNone && subtitleInputFile.IsNone ||
                                                            currentState.ScaledSize != desiredState.ScaledSize =>
                    new DecoderVaapi(),

                // videotoolbox should use implicit decoders
                (HardwareAccelerationMode.VideoToolbox, _, _) => new DecoderVideoToolbox(),

                (_, VideoFormat.Hevc, _) => new DecoderHevc(),
                (_, VideoFormat.H264, _) => new DecoderH264(),
                (_, VideoFormat.Mpeg1Video, _) => new DecoderMpeg1Video(),
                (_, VideoFormat.Mpeg2Video, _) => new DecoderMpeg2Video(),
                (_, VideoFormat.Vc1, _) => new DecoderVc1(),
                (_, VideoFormat.MsMpeg4V2, _) => new DecoderMsMpeg4V2(),
                (_, VideoFormat.MsMpeg4V3, _) => new DecoderMsMpeg4V3(),
                (_, VideoFormat.Mpeg4, _) => new DecoderMpeg4(),
                (_, VideoFormat.Vp9, _) => new DecoderVp9(),

                (_, VideoFormat.Undetermined, _) => new DecoderImplicit(),
                (_, VideoFormat.Copy, _) => new DecoderImplicit(),
                (_, VideoFormat.GeneratedImage, _) => new DecoderImplicit(),

                var (accel, videoFormat, pixelFormat) => LogUnknownDecoder(accel, videoFormat, pixelFormat, logger)
            };

    private static Option<IDecoder> LogUnknownDecoder(
        HardwareAccelerationMode hardwareAccelerationMode,
        string videoFormat,
        string pixelFormat,
        ILogger logger)
    {
        logger.LogWarning(
            "Unable to determine decoder for {AccelMode} - {VideoFormat} - {PixelFormat}; may have playback issues",
            hardwareAccelerationMode,
            videoFormat,
            pixelFormat);
        return Option<IDecoder>.None;
    }
}
