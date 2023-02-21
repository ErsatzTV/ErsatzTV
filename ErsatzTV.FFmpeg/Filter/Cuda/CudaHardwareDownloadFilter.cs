using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class CudaHardwareDownloadFilter : BaseFilter
{
    private readonly Option<IPixelFormat> _maybeCurrentPixelFormat;
    private readonly Option<IPixelFormat> _maybeTargetPixelFormat;

    public CudaHardwareDownloadFilter(
        Option<IPixelFormat> maybeCurrentPixelFormat,
        Option<IPixelFormat> maybeTargetPixelFormat)
    {
        _maybeCurrentPixelFormat = maybeCurrentPixelFormat;
        _maybeTargetPixelFormat = maybeTargetPixelFormat;
    }

    public override string Filter
    {
        get
        {
            var hwdownload = "hwdownload";
            foreach (IPixelFormat pixelFormat in _maybeCurrentPixelFormat)
            {
                if (!string.IsNullOrWhiteSpace(pixelFormat.FFmpegName))
                {
                    hwdownload += $",format={pixelFormat.FFmpegName}";

                    if (pixelFormat is PixelFormatNv12 nv12)
                    {
                        if (_maybeTargetPixelFormat.IsNone)
                        {
                            foreach (IPixelFormat pf in AvailablePixelFormats.ForPixelFormat(nv12.Name, null))
                            {
                                hwdownload += $",format={pf.FFmpegName}";
                            }
                        }
                        else
                        {
                            foreach (IPixelFormat pf in _maybeTargetPixelFormat)
                            {
                                hwdownload += $",format={pf.FFmpegName}";
                            }
                        }
                    }
                }
            }

            return hwdownload;
        }
    }

    public override FrameState NextState(FrameState currentState)
    {
        FrameState result = currentState with
        {
            FrameDataLocation = FrameDataLocation.Software
        };

        foreach (IPixelFormat pixelFormat in _maybeTargetPixelFormat)
        {
            result = result with { PixelFormat = Some(pixelFormat) };
            return result;
        }

        foreach (IPixelFormat pixelFormat in _maybeCurrentPixelFormat)
        {
            if (pixelFormat is PixelFormatNv12 nv12)
            {
                result = result with { PixelFormat = AvailablePixelFormats.ForPixelFormat(nv12.Name, null) };
            }
            else
            {
                result = result with { PixelFormat = Some(pixelFormat) };
            }
        }

        return result;
    }
}
