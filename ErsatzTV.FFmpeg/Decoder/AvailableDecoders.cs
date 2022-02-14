﻿using ErsatzTV.FFmpeg.Decoder.Cuvid;
using ErsatzTV.FFmpeg.Decoder.Qsv;
using ErsatzTV.FFmpeg.Format;
using LanguageExt;

namespace ErsatzTV.FFmpeg.Decoder;

public static class AvailableDecoders
{
    public static Option<IDecoder> ForVideoFormat(FrameState currentState, FrameState desiredState)
    {
        return (currentState.HardwareAccelerationMode, currentState.VideoFormat,
                currentState.PixelFormat.Match(pf => pf.Name, () => string.Empty)) switch
            {
                (HardwareAccelerationMode.Nvenc, VideoFormat.Hevc, _) => new DecoderHevcCuvid(desiredState),

                // nvenc doesn't support hardware decoding of 10-bit content
                (HardwareAccelerationMode.Nvenc, VideoFormat.H264, PixelFormat.YUV420P10LE or PixelFormat.YUV444P10LE)
                    => new DecoderH264(),

                (HardwareAccelerationMode.Nvenc, VideoFormat.H264, _) => new DecoderH264Cuvid(desiredState),
                (HardwareAccelerationMode.Nvenc, VideoFormat.Mpeg2Video, _) => new DecoderMpeg2Cuvid(desiredState),
                (HardwareAccelerationMode.Nvenc, VideoFormat.Vc1, _) => new DecoderVc1Cuvid(desiredState),
                (HardwareAccelerationMode.Nvenc, VideoFormat.Vp9, _) => new DecoderVp9Cuvid(desiredState),
                (HardwareAccelerationMode.Nvenc, VideoFormat.Mpeg4, _) => new DecoderMpeg4Cuvid(desiredState),

                // hevc_qsv decoder sometimes causes green lines with 10-bit content
                (HardwareAccelerationMode.Qsv, VideoFormat.Hevc, PixelFormat.YUV420P10LE) => new DecoderHevc(),

                (HardwareAccelerationMode.Qsv, VideoFormat.Hevc, _) => new DecoderHevcQsv(),
                (HardwareAccelerationMode.Qsv, VideoFormat.H264, _) => new DecoderH264Qsv(),
                (HardwareAccelerationMode.Qsv, VideoFormat.Mpeg2Video, _) => new DecoderMpeg2Qsv(),
                (HardwareAccelerationMode.Qsv, VideoFormat.Vc1, _) => new DecoderVc1Qsv(),
                (HardwareAccelerationMode.Qsv, VideoFormat.Vp9, _) => new DecoderVp9Qsv(),

                // vaapi should use implicit decoders
                (HardwareAccelerationMode.Vaapi, _, _) => new DecoderVaapi(),

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

                // TODO: log warning and fall back to automatic decoder (i.e. None) instead of throwing?
                // maybe have a special "unknown decoder" that sets frame loc to software without specifying any decoder
                _ => throw new ArgumentOutOfRangeException(
                    nameof(currentState.VideoFormat),
                    currentState.VideoFormat,
                    null)
            };
    }
}
