using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class TonemapQsvFilter(IPixelFormat desiredPixelFormat) : BaseFilter
{
    public override string Filter =>
        desiredPixelFormat.BitDepth == 8
            ? $"vpp_qsv=tonemap=1:format=nv12"
            : $"vpp_qsv=tonemap=1:format=p010le";

    public override FrameState NextState(FrameState currentState)
    {
        return desiredPixelFormat.BitDepth == 8
                ? currentState with { FrameDataLocation = FrameDataLocation.Hardware, PixelFormat = new PixelFormatNv12(desiredPixelFormat.Name) }
                : currentState with { FrameDataLocation = FrameDataLocation.Hardware, PixelFormat = new PixelFormatP010() };
    }

}
