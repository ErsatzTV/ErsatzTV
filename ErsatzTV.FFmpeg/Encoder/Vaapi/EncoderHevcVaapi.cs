using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderHevcVaapi : EncoderBase
{
    private readonly FrameState _currentState;
    private readonly Option<WatermarkInputFile> _maybeWatermarkInputFile;

    public EncoderHevcVaapi(FrameState currentState, Option<WatermarkInputFile> maybeWatermarkInputFile)
    {
        _currentState = currentState;
        _maybeWatermarkInputFile = maybeWatermarkInputFile;
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc
        // don't change the frame data location
    };

    public override string Name => "hevc_vaapi";
    public override StreamKind Kind => StreamKind.Video;

    // need to upload if we're still in software unless a watermark is used
    public override string Filter
    {
        get
        {
            if (_maybeWatermarkInputFile.IsNone && _currentState.FrameDataLocation == FrameDataLocation.Software)
            {
                return "format=nv12|vaapi,hwupload";
            }

            return string.Empty;
        }
    }
}
