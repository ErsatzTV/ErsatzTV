using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.Preset;

namespace ErsatzTV.Core.FFmpeg;

public static class FFmpegLibraryHelper
{
    public static ICollection<string> PresetsForFFmpegProfile(
        HardwareAccelerationKind hardwareAccelerationKind,
        FFmpegProfileVideoFormat videoFormat,
        FFmpegProfileBitDepth bitDepth) => AvailablePresets.ForAccelAndFormat(
        MapAccel(hardwareAccelerationKind),
        MapVideoFormat(videoFormat),
        MapBitDepth(bitDepth));

    public static HardwareAccelerationMode MapAccel(HardwareAccelerationKind kind) =>
        kind switch
        {
            HardwareAccelerationKind.Amf => HardwareAccelerationMode.Amf,
            HardwareAccelerationKind.Nvenc => HardwareAccelerationMode.Nvenc,
            HardwareAccelerationKind.Qsv => HardwareAccelerationMode.Qsv,
            HardwareAccelerationKind.Vaapi => HardwareAccelerationMode.Vaapi,
            HardwareAccelerationKind.VideoToolbox => HardwareAccelerationMode.VideoToolbox,
            HardwareAccelerationKind.V4l2m2m => HardwareAccelerationMode.V4l2m2m,
            HardwareAccelerationKind.Rkmpp => HardwareAccelerationMode.Rkmpp,
            _ => HardwareAccelerationMode.None
        };

    public static string MapVideoFormat(FFmpegProfileVideoFormat format) =>
        format switch
        {
            FFmpegProfileVideoFormat.H264 => VideoFormat.H264,
            FFmpegProfileVideoFormat.Hevc => VideoFormat.Hevc,
            FFmpegProfileVideoFormat.Av1 => VideoFormat.Av1,
            _ => VideoFormat.Mpeg2Video
        };

    public static int MapBitDepth(FFmpegProfileBitDepth bitDepth) =>
        bitDepth switch
        {
            FFmpegProfileBitDepth.EightBit => 8,
            _ => 10
        };
}
