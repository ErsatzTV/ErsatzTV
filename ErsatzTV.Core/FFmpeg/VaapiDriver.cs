using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ErsatzTV.Core.FFmpeg;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[JsonConverter(typeof(JsonStringEnumConverter<VaapiDriver>))]
public enum VaapiDriver
{
    Default = 0,
    iHD = 1,
    i965 = 2,
    RadeonSI = 3,
    Nouveau = 4
}
