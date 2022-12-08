namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class HardwareUploadVaapiFilter : BaseFilter
{
    private readonly bool _setFormat;

    public HardwareUploadVaapiFilter(bool setFormat) => _setFormat = setFormat;

    public override string Filter => _setFormat switch
    {
        false => "hwupload",
        true => "format=nv12|p010le|vaapi,hwupload"
    };

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Hardware };
}
