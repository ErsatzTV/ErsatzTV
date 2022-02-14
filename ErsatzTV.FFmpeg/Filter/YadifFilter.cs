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
                switch (_currentState.PixelFormat.FFmpegName)
                {
                    case FFmpegFormat.NV12:
                        hwdownload = "hwdownload,format=nv12,";
                        break;
                    default:
                        hwdownload = "hwdownload,";
                        break;
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