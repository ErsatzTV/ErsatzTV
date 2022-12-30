namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class DeinterlaceVaapiFilter : BaseFilter
{
    private readonly FrameState _currentState;

    public DeinterlaceVaapiFilter(FrameState currentState) => _currentState = currentState;

    public override string Filter =>
        _currentState.FrameDataLocation == FrameDataLocation.Hardware
            ? "deinterlace_vaapi"
            : "format=nv12|p010le|vaapi,hwupload,deinterlace_vaapi";

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        Deinterlaced = true,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
