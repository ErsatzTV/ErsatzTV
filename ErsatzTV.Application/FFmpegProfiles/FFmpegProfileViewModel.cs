using ErsatzTV.Application.Resolutions;

namespace ErsatzTV.Application.FFmpegProfiles
{
    public record FFmpegProfileViewModel(
        int Id,
        string Name,
        int ThreadCount,
        bool Transcode,
        ResolutionViewModel Resolution,
        bool NormalizeResolution,
        string VideoCodec,
        bool NormalizeVideoCodec,
        int VideoBitrate,
        int VideoBufferSize,
        string AudioCodec,
        bool NormalizeAudioCodec,
        int AudioBitrate,
        int AudioBufferSize,
        int AudioVolume,
        int AudioChannels,
        int AudioSampleRate,
        bool NormalizeAudio);
}
