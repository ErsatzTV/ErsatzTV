namespace ErsatzTV.FFmpeg.Capabilities;

public record FFmpegKnownHardwareAcceleration
{
    public static readonly FFmpegKnownHardwareAcceleration Amf = new("amf");
    public static readonly FFmpegKnownHardwareAcceleration Cuda = new("cuda");
    public static readonly FFmpegKnownHardwareAcceleration Qsv = new("qsv");
    public static readonly FFmpegKnownHardwareAcceleration Vaapi = new("vaapi");
    public static readonly FFmpegKnownHardwareAcceleration VideoToolbox = new("videotoolbox");
    public static readonly FFmpegKnownHardwareAcceleration OpenCL = new("opencl");
    public static readonly FFmpegKnownHardwareAcceleration Vulkan = new("vulkan");
    public static readonly FFmpegKnownHardwareAcceleration V4l2m2m = new("v4l2m2m");

    private FFmpegKnownHardwareAcceleration(string Name) => this.Name = Name;

    public string Name { get; }

    public static IList<string> AllAccels =>
    [
        Amf.Name,
        Cuda.Name,
        Qsv.Name,
        Vaapi.Name,
        VideoToolbox.Name,
        OpenCL.Name,
        Vulkan.Name,
        V4l2m2m.Name
    ];
}
