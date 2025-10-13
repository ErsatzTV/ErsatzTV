using System.Text.Json.Serialization;

namespace ErsatzTV.Core.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<ScalingBehavior>))]
public enum ScalingBehavior
{
    ScaleAndPad = 0,
    Stretch = 1,
    Crop = 2
}
