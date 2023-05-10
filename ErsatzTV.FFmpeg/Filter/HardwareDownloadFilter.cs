using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class HardwareDownloadFilter : BaseFilter
{
    private readonly FrameState _currentState;

    public HardwareDownloadFilter(FrameState currentState) => _currentState = currentState;

    public override string Filter
    {
        get
        {
            string hwdownload = string.Empty;
            if (_currentState.FrameDataLocation == FrameDataLocation.Hardware)
            {
                hwdownload = "hwdownload";
                foreach (IPixelFormat pixelFormat in _currentState.PixelFormat)
                {
                    if (pixelFormat is PixelFormatVaapi)
                    {
                        foreach (IPixelFormat pf in AvailablePixelFormats.ForPixelFormat(pixelFormat.Name, null))
                        {
                            return $"hwdownload,format=vaapi|{pf.FFmpegName}";
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(pixelFormat.FFmpegName))
                    {
                        hwdownload = $"hwdownload,format={pixelFormat.FFmpegName}";
                    }
                }
            }

            return hwdownload;
        }
    }

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Software };
}
