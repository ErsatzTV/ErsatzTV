using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Graphics;

public class BaseGraphicsElement
{
    public string Name { get; set; }

    [YamlIgnore]
    public string SourceFileName { get; set; }

    public string DebugName() => string.IsNullOrEmpty(Name) ? SourceFileName : Name;
}
