using System.Text.Json.Serialization;

namespace ErsatzTV.Core.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<FFmpegProfileTonemapAlgorithm>))]
public enum FFmpegProfileTonemapAlgorithm
{
    Linear = 0,
    Clip = 1,
    Gamma = 2,
    Reinhard = 3,
    Mobius = 4,
    Hable = 5
}
