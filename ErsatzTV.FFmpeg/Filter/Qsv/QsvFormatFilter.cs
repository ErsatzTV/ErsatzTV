using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Qsv;

public class QsvFormatFilter : BaseFilter
{
    private readonly IPixelFormat _pixelFormat;

    public QsvFormatFilter(IPixelFormat pixelFormat) => _pixelFormat = pixelFormat;

    public override FrameState NextState(FrameState currentState) => currentState with { PixelFormat = Some(_pixelFormat) };

    public override string Filter => $"vpp_qsv=format={_pixelFormat.FFmpegName}";
}
