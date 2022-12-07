using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Filter;

public class OverlayWatermarkFilter : BaseFilter
{
    private readonly FrameSize _resolution;
    private readonly FrameSize _squarePixelFrameSize;
    private readonly IPixelFormat _outputPixelFormat;
    private readonly ILogger _logger;
    private readonly WatermarkState _watermarkState;

    public OverlayWatermarkFilter(
        WatermarkState watermarkState,
        FrameSize resolution,
        FrameSize squarePixelFrameSize,
        IPixelFormat outputPixelFormat,
        ILogger logger)
    {
        _watermarkState = watermarkState;
        _resolution = resolution;
        _squarePixelFrameSize = squarePixelFrameSize;
        _outputPixelFormat = outputPixelFormat;
        _logger = logger;
    }

    public override string Filter => $"overlay={Position}:format={(_outputPixelFormat.BitDepth == 10 ? '1' : '0')}";

    protected string Position
    {
        get
        {
            (double horizontalMargin, double verticalMargin) = _watermarkState.PlaceWithinSourceContent
                ? SourceContentMargins()
                : NormalMargins();

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

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Software };

    private WatermarkMargins NormalMargins()
    {
        double horizontalMargin = Math.Round(_watermarkState.HorizontalMarginPercent / 100.0 * _resolution.Width);
        double verticalMargin = Math.Round(_watermarkState.VerticalMarginPercent / 100.0 * _resolution.Height);

        return new WatermarkMargins(horizontalMargin, verticalMargin);
    }

    private WatermarkMargins SourceContentMargins()
    {
        int horizontalPadding = _resolution.Width - _squarePixelFrameSize.Width;
        int verticalPadding = _resolution.Height - _squarePixelFrameSize.Height;

        _logger.LogDebug("Resolution: {Width}x{Height}", _resolution.Width, _resolution.Height);
        _logger.LogDebug("Square Pix: {Width}x{Height}", _squarePixelFrameSize.Width, _squarePixelFrameSize.Height);    

        double horizontalMargin = Math.Round(
            _watermarkState.HorizontalMarginPercent / 100.0 * _squarePixelFrameSize.Width
            + horizontalPadding / 2.0);
        double verticalMargin = Math.Round(
            _watermarkState.VerticalMarginPercent / 100.0 * _squarePixelFrameSize.Height
            + verticalPadding / 2.0);

        return new WatermarkMargins(horizontalMargin, verticalMargin);
    }

    private record WatermarkMargins(double HorizontalMargin, double VerticalMargin);
}
