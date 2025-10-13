using System.Text.Json.Serialization;

namespace ErsatzTV.Core.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<FFmpegProfileBitDepth>))]
public enum FFmpegProfileBitDepth
{
    EightBit = 0,
    TenBit = 1
}
