namespace ErsatzTV.FFmpeg.Environment;

public class CudaVisibleDevicesVariable(string visibleDevices) : IPipelineStep
{
    public EnvironmentVariable[] EnvironmentVariables => [ new("CUDA_VISIBLE_DEVICES", visibleDevices) ];
    public string[] GlobalOptions => [];
    public string[] InputOptions(InputFile inputFile) => [];
    public string[] FilterOptions => [];
    public string[] OutputOptions => [];
    public FrameState NextState(FrameState currentState) => currentState;
}
