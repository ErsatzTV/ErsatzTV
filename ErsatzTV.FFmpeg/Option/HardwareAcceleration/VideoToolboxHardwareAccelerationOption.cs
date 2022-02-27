namespace ErsatzTV.FFmpeg.Option.HardwareAcceleration;

public class VideoToolboxHardwareAccelerationOption : GlobalOption
{
    public override IList<string> GlobalOptions => new List<string> { "-hwaccel", "videotoolbox" };
        
    public override FrameState NextState(FrameState currentState) => currentState with
    {
        FrameDataLocation = FrameDataLocation.Software
    };
}
