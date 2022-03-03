using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Nvenc;

public class EncoderH264Nvenc : EncoderBase
{
    private readonly FrameState _currentState;
    private readonly Option<WatermarkInputFile> _maybeWatermarkInputFile;

    public EncoderH264Nvenc(FrameState currentState, Option<WatermarkInputFile> maybeWatermarkInputFile)
    {
        _currentState = currentState;
        _maybeWatermarkInputFile = maybeWatermarkInputFile;
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264,
        FrameDataLocation = FrameDataLocation.Hardware
    };

    public override string Name => "h264_nvenc";
    public override StreamKind Kind => StreamKind.Video;
    
    // need to upload if we're still in software and a watermark is used
    public override string Filter
    {
        get
        {
            // only upload to hw if we need to overlay a watermark
            if (_maybeWatermarkInputFile.IsSome && _currentState.FrameDataLocation == FrameDataLocation.Software)
            {
                return "hwupload_cuda";
            }

            return string.Empty;
        }
    }
}
