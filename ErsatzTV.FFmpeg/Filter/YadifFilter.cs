using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class YadifFilter : BaseFilter
{
    private readonly FrameState _currentState;

    public YadifFilter(FrameState currentState)
    {
        _currentState = currentState;
    }

    public override string Filter
    {
        get
        {
            string hwdownload = string.Empty;
            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                hwdownload = "hwdownload,";
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    if (pixelFormat.FFmpegName == FFmpegFormat.NV12)
                    {
                        hwdownload = "hwdownload,format=nv12,";
                    }
                }
            }

            return $"{hwdownload}yadif=1";
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        Deinterlaced = true,
        FrameDataLocation = FrameDataLocation.Software
    };
}