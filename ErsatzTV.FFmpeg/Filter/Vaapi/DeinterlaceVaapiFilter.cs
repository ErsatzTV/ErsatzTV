namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class DeinterlaceVaapiFilter : BaseFilter
{
    private readonly FrameState _currentState;

    public DeinterlaceVaapiFilter(FrameState currentState) => _currentState = currentState;

    private static string GetDeinterlaceFilter()
    {
        string? modeEnv = System.Environment.GetEnvironmentVariable("VAAPI_DEINT_MODE");
        
        if (!string.IsNullOrEmpty(modeEnv) && int.TryParse(modeEnv, out int mode))
        {
            return $"deinterlace_vaapi=mode={mode}";
        }
        
        // No mode specified - let driver pick default
        return "deinterlace_vaapi";
    }

    public override string Filter =>
        _currentState.FrameDataLocation == FrameDataLocation.Hardware
            ? GetDeinterlaceFilter()
            : $"format=nv12|p010le|vaapi,hwupload,{GetDeinterlaceFilter()}";

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        Deinterlaced = true,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}