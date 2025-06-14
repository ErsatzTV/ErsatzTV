namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class PadVaapiFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly FrameSize _paddedSize;

    public PadVaapiFilter(FrameState currentState, FrameSize paddedSize)
    {
        _currentState = currentState;
        _paddedSize = paddedSize;
    }

    public override string Filter
    {
        get
        {
            var pad = $"pad_vaapi=w={_paddedSize.Width}:h={_paddedSize.Height}:x=-1:y=-1:color=black";

            return _currentState.FrameDataLocation == FrameDataLocation.Hardware
                ? pad
                : $"format=nv12|p010le|vaapi,hwupload,{pad}";
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        PaddedSize = _paddedSize,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
