using System.Text.Json.Serialization;

namespace ErsatzTV.Core.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<HardwareAccelerationKind>))]
public enum HardwareAccelerationKind
{
    None = 0,
    Qsv = 1,
    Nvenc = 2,
    Vaapi = 3,
    VideoToolbox = 4,
    Amf = 5,
    V4l2m2m = 6,
    Rkmpp = 7
}
