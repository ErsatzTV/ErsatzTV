using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class CropFilter : BaseFilter
{
    private readonly FrameSize _croppedSize;
    private readonly FrameState _currentState;

    public CropFilter(FrameState currentState, FrameSize croppedSize)
    {
        _currentState = currentState;
        _croppedSize = croppedSize;
    }

    public override string Filter
    {
        get
        {
            var crop = $"crop=w={_croppedSize.Width}:h={_croppedSize.Height}";

            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    if (pixelFormat is PixelFormatVaapi)
                    {
                        foreach (IPixelFormat pf in AvailablePixelFormats.ForPixelFormat(pixelFormat.Name, null))
                        {
                            return $"hwdownload,format=vaapi|{pf.FFmpegName},{crop}";
                        }
                    }

                    return $"hwdownload,format={pixelFormat.FFmpegName},{crop}";
                }

                return $"hwdownload,{crop}";
            }

            return crop;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        PaddedSize = _croppedSize,
        FrameDataLocation = FrameDataLocation.Software
    };
}
