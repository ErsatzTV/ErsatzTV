using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class CudaHardwareDownloadFilter : BaseFilter
{
    private readonly Option<IPixelFormat> _maybePixelFormat;

    public CudaHardwareDownloadFilter(Option<IPixelFormat> maybePixelFormat) => _maybePixelFormat = maybePixelFormat;

    public override string Filter
    {
        get
        {
            var hwdownload = "hwdownload";
            foreach (IPixelFormat pixelFormat in _maybePixelFormat)
            {
                if (!string.IsNullOrWhiteSpace(pixelFormat.FFmpegName))
                {
                    hwdownload += $",format={pixelFormat.FFmpegName}";

                    if (pixelFormat is PixelFormatNv12 nv12)
                    {
                        foreach (IPixelFormat pf in AvailablePixelFormats.ForPixelFormat(nv12.Name, null))
                        {
                            hwdownload += $",format={pf.FFmpegName}";
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
        
        foreach (IPixelFormat pixelFormat in _maybePixelFormat)
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
