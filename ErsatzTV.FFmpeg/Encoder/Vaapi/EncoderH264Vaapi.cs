using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderH264Vaapi : EncoderBase
{
    private readonly FrameState _currentState;
    private readonly Option<SubtitleInputFile> _maybeSubtitleInputFile;
    private readonly Option<WatermarkInputFile> _maybeWatermarkInputFile;

    public EncoderH264Vaapi(
        FrameState currentState,
        Option<WatermarkInputFile> maybeWatermarkInputFile,
        Option<SubtitleInputFile> maybeSubtitleInputFile)
    {
        _currentState = currentState;
        _maybeWatermarkInputFile = maybeWatermarkInputFile;
        _maybeSubtitleInputFile = maybeSubtitleInputFile;
    }

    public override string Name => "h264_vaapi";
    public override StreamKind Kind => StreamKind.Video;

    // need to upload if we're still in software unless a watermark or subtitle is used
    public override string Filter
    {
        get
        {
            if (_currentState.FrameDataLocation == FrameDataLocation.Software)
            {
                if (_maybeWatermarkInputFile.IsNone && _maybeSubtitleInputFile.IsNone)
                {
                    return "format=nv12|vaapi,hwupload";
                }
            }

            return string.Empty;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264
        // don't change the frame data location
    };
}
