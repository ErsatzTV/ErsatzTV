namespace ErsatzTV.Core.Api.FFmpegProfiles;

public record FFmpegFullProfileResponseModel(
    int Id,
    string Name,
    int ThreadCount,
    int HardwareAcceleration,
    int VaapiDriver,
    string VaapiDevice,
    int ResolutionId,
    int VideoFormat,
    int VideoBitrate,
    int VideoBufferSize,
    int AudioFormat,
    int AudioBitrate,
    int AudioBufferSize,
    int NormalizeLoudnessMode,
    int AudioChannels,
    int AudioSampleRate,
    bool NormalizeFramerate,
    bool? DeinterlaceVideo);
