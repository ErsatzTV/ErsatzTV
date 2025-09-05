namespace ErsatzTV.FFmpeg;

public enum HardwareAccelerationMode
{
    None = 0,
    Qsv = 1,
    Nvenc = 2,
    Vaapi = 3,
    VideoToolbox = 4,
    Amf = 5,
    OpenCL = 6,
    Vulkan = 7,
    V4l2m2m = 8,
    Rkmpp = 9
}
