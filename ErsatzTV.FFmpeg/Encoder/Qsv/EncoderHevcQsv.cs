using ErsatzTV.FFmpeg.Format;
using LanguageExt;

namespace ErsatzTV.FFmpeg.Encoder.Qsv;

public class EncoderHevcQsv : EncoderBase
{
    private readonly FrameState _currentState;
    private readonly Option<WatermarkInputFile> _maybeWatermarkInputFile;

    public EncoderHevcQsv(FrameState currentState, Option<WatermarkInputFile> maybeWatermarkInputFile)
    {
        _currentState = currentState;
        _maybeWatermarkInputFile = maybeWatermarkInputFile;
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc,
        FrameDataLocation = FrameDataLocation.Hardware
    };

    public override string Name => "hevc_qsv";
    public override StreamKind Kind => StreamKind.Video;
    
    // need to upload if we're still in software and a watermark is used
    public override string Filter
    {
        get
        {
            // only upload to hw if we need to overlay a watermark
            if (_maybeWatermarkInputFile.IsSome && _currentState.FrameDataLocation == FrameDataLocation.Software)
            {
                // pixel format should already be converted to a supported format by QsvHardwareAccelerationOption
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    return $"format={pixelFormat.FFmpegName},hwupload=extra_hw_frames=64";
                }

                // default to nv12
                return "format=nv12,hwupload=extra_hw_frames=64";
            }

            return string.Empty;
        }
    }
}
