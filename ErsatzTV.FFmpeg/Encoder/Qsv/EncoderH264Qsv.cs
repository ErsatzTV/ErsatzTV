using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Qsv;

public class EncoderH264Qsv : EncoderBase
{
    private readonly FrameState _currentState;
    private readonly Option<SubtitleInputFile> _maybeSubtitleInputFile;
    private readonly Option<WatermarkInputFile> _maybeWatermarkInputFile;

    public EncoderH264Qsv(
        FrameState currentState,
        Option<WatermarkInputFile> maybeWatermarkInputFile,
        Option<SubtitleInputFile> maybeSubtitleInputFile)
    {
        _currentState = currentState;
        _maybeWatermarkInputFile = maybeWatermarkInputFile;
        _maybeSubtitleInputFile = maybeSubtitleInputFile;
    }

    public override string Name => "h264_qsv";
    public override StreamKind Kind => StreamKind.Video;
    public override IList<string> OutputOptions => new[] { "-c:v", "h264_qsv", "-low_power", "0" };

    // need to upload if we're still in software and a watermark is used
    public override string Filter
    {
        get
        {
            // only upload to hw if we need to overlay (watermark or subtitle)
            if (_currentState.FrameDataLocation == FrameDataLocation.Software)
            {
                bool isPictureSubtitle = _maybeSubtitleInputFile.Map(s => s.IsImageBased).IfNone(false);

                if (isPictureSubtitle || _maybeWatermarkInputFile.IsSome)
                {
                    // pixel format should already be converted to a supported format by QsvHardwareAccelerationOption
                    foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                    {
                        return $"format={pixelFormat.FFmpegName},hwupload=extra_hw_frames=64";
                    }

                    // default to nv12
                    return "format=nv12,hwupload=extra_hw_frames=64";
                }
            }

            return string.Empty;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
