using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class TonemapVaapiFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly IPixelFormat _desiredPixelFormat;

    public TonemapVaapiFilter(FrameState currentState, IPixelFormat desiredPixelFormat)
    {
        _currentState = currentState;
        _desiredPixelFormat = desiredPixelFormat;
    }

    public override string Filter => $"tonemap_vaapi=format={_desiredPixelFormat.FFmpegName}";

    public override FrameState NextState(FrameState currentState) =>
        currentState with
        {
            PixelFormat = Some(_desiredPixelFormat),
            FrameDataLocation = FrameDataLocation.Hardware
        };
}
