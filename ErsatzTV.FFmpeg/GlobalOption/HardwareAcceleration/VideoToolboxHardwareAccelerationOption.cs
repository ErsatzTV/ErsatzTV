namespace ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;

public class VideoToolboxHardwareAccelerationOption : GlobalOption
{
    public override string[] GlobalOptions => new[] { "-hwaccel", "videotoolbox" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        FrameDataLocation = FrameDataLocation.Software
    };
}
