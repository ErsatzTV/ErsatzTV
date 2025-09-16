using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Preset;

public static class AvailablePresets
{
    public static ICollection<string> ForAccelAndFormat(
        HardwareAccelerationMode hardwareAccelerationMode,
        string videoFormat,
        int bitDepth) =>
        (hardwareAccelerationMode, videoFormat, bitDepth) switch
        {
            // 10-bit h264 always uses libx264
            (_, VideoFormat.H264, 10) =>
            [
                VideoPreset.VeryFast
            ],

            (HardwareAccelerationMode.Nvenc, VideoFormat.H264, 8) =>
            [
                VideoPreset.LowLatencyHighPerformance, VideoPreset.LowLatencyHighQuality
            ],

            (HardwareAccelerationMode.Nvenc, VideoFormat.Hevc, _) =>
            [
                VideoPreset.LowLatencyHighPerformance, VideoPreset.LowLatencyHighQuality
            ],

            (HardwareAccelerationMode.Qsv, VideoFormat.H264 or VideoFormat.Hevc, _) =>
            [
                VideoPreset.VeryFast
            ],

            (HardwareAccelerationMode.None, VideoFormat.H264 or VideoFormat.Hevc, _) =>
            [
                VideoPreset.VeryFast
            ],

            _ => Array.Empty<string>()
        };
}
