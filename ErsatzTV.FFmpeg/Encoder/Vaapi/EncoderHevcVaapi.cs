using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderHevcVaapi : EncoderBase
{
    private readonly FrameState _currentState;
    private readonly Option<WatermarkInputFile> _maybeWatermarkInputFile;
    private readonly Option<SubtitleInputFile> _maybeSubtitleInputFile;

    public EncoderHevcVaapi(
        FrameState currentState,
        Option<WatermarkInputFile> maybeWatermarkInputFile,
        Option<SubtitleInputFile> maybeSubtitleInputFile)
    {
        _currentState = currentState;
        _maybeWatermarkInputFile = maybeWatermarkInputFile;
        _maybeSubtitleInputFile = maybeSubtitleInputFile;
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc
        // don't change the frame data location
    };

    public override string Name => "hevc_vaapi";
    public override StreamKind Kind => StreamKind.Video;

    // need to upload if we're still in software unless a watermark or picture subtitle is used
    public override string Filter
    {
        get
        {
            if (_currentState.FrameDataLocation == FrameDataLocation.Software)
            {
                bool isNotImageSubtitle = _maybeSubtitleInputFile.Map(s => s.IsImageBased).IfNone(false) == false; 
                
                if (_maybeWatermarkInputFile.IsNone && isNotImageSubtitle)
                {
                    return "format=nv12|vaapi,hwupload";
                }
            }

            return string.Empty;
        }
    }
}
