using System.Text.Json.Serialization;

namespace ErsatzTV.Core.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<FFmpegProfileAudioFormat>))]
public enum FFmpegProfileAudioFormat
{
    None = 0,

    Aac = 1,
    Ac3 = 2,
    AacLatm = 3,

    Copy = 99
}
