using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter;

public class OverlayFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly WatermarkState _watermarkState;
    private readonly FrameSize _resolution;

    public OverlayFilter(FrameState currentState, WatermarkState watermarkState, FrameSize resolution)
    {
        _currentState = currentState;
        _watermarkState = watermarkState;
        _resolution = resolution;
    }

    public override FrameState NextState(FrameState currentState) => currentState;

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

            return $"{hwdownload}overlay={Position}";
        }
    }

    protected string Position
    {
        get
        {
            double horizontalMargin = Math.Round(_watermarkState.HorizontalMarginPercent / 100.0 * _resolution.Width);
            double verticalMargin = Math.Round(_watermarkState.VerticalMarginPercent / 100.0 * _resolution.Height);

            return _watermarkState.Location switch
            {
                WatermarkLocation.BottomLeft => $"x={horizontalMargin}:y=H-h-{verticalMargin}",
                WatermarkLocation.TopLeft => $"x={horizontalMargin}:y={verticalMargin}",
                WatermarkLocation.TopRight => $"x=W-w-{horizontalMargin}:y={verticalMargin}",
                WatermarkLocation.TopMiddle => $"x=(W-w)/2:y={verticalMargin}",
                WatermarkLocation.RightMiddle => $"x=W-w-{horizontalMargin}:y=(H-h)/2",
                WatermarkLocation.BottomMiddle => $"x=(W-w)/2:y=H-h-{verticalMargin}",
                WatermarkLocation.LeftMiddle => $"x={horizontalMargin}:y=(H-h)/2",
                _ => $"x=W-w-{horizontalMargin}:y=H-h-{verticalMargin}"
            };
        }
    }
}
