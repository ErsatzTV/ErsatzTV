using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Filter;

public class OverlayWatermarkFilter : BaseFilter
{
    private readonly FrameSize _resolution;
    private readonly FrameSize _squarePixelFrameSize;
    private readonly ILogger _logger;
    private readonly WatermarkState _watermarkState;

    public OverlayWatermarkFilter(
        WatermarkState watermarkState,
        FrameSize resolution,
        FrameSize squarePixelFrameSize,
        ILogger logger)
    {
        _watermarkState = watermarkState;
        _resolution = resolution;
        _squarePixelFrameSize = squarePixelFrameSize;
        _logger = logger;
    }

    public override string Filter => $"overlay={Position}";

    protected string Position
    {
        get
        {
            int horizontalPadding = _resolution.Width - _squarePixelFrameSize.Width;
            int verticalPadding = _resolution.Height - _squarePixelFrameSize.Height;

            _logger.LogDebug(
                $"Resolution: {_resolution.Width}x{_resolution.Height}");
            _logger.LogDebug(
                $"Square Pix: {_squarePixelFrameSize.Width}x{_squarePixelFrameSize.Height}");    

            double horizontalMargin = Math.Round(
                _watermarkState.HorizontalMarginPercent / 100.0 * _squarePixelFrameSize.Width
                + horizontalPadding / 2.0);
            double verticalMargin = Math.Round(
                _watermarkState.VerticalMarginPercent / 100.0 * _squarePixelFrameSize.Height
                + verticalPadding / 2.0);

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

    public override FrameState NextState(FrameState currentState) => currentState;
}
