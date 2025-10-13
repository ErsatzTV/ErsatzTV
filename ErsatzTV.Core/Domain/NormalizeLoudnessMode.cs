using System.Text.Json.Serialization;

namespace ErsatzTV.Core.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<NormalizeLoudnessMode>))]
public enum NormalizeLoudnessMode
{
    Off = 0,
    LoudNorm = 1
}
