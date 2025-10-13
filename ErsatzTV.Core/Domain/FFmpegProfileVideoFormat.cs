using System.Text.Json.Serialization;

namespace ErsatzTV.Core.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<FFmpegProfileVideoFormat>))]
public enum FFmpegProfileVideoFormat
{
    None = 0,

    H264 = 1,
    Hevc = 2,
    Mpeg2Video = 3,
    Av1 = 4,

    Copy = 99
}
