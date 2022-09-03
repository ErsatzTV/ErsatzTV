namespace ErsatzTV.FFmpeg;

public enum HardwareAccelerationMode
{
    None = 0,
    Qsv = 1,
    Nvenc = 2,
    Vaapi = 3,
    VideoToolbox = 4,
    Amf = 5
}
