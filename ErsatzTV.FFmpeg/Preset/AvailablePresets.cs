using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Preset;

public static class AvailablePresets
{
    public static ICollection<string> ForAccelAndFormat(
        HardwareAccelerationMode hardwareAccelerationMode,
        string videoFormat) =>
        (hardwareAccelerationMode, videoFormat) switch
        {
            (HardwareAccelerationMode.Nvenc, VideoFormat.H264 or VideoFormat.Hevc) =>
            [
                VideoPreset.LowLatencyHighPerformance, VideoPreset.LowLatencyHighQuality
            ],

            (HardwareAccelerationMode.Qsv, VideoFormat.H264 or VideoFormat.Hevc) =>
            [
                VideoPreset.VeryFast
            ],

            (HardwareAccelerationMode.None, VideoFormat.H264 or VideoFormat.Hevc) =>
            [
                VideoPreset.VeryFast
            ],

            _ => Array.Empty<string>()
        };
}
