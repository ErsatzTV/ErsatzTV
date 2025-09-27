namespace ErsatzTV.FFmpeg.Capabilities.Nvidia;

public record CudaDevice(int Handle, string Model, Version Version);
