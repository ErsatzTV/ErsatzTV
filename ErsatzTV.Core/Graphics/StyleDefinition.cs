using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Graphics;

public class StyleDefinition
{
    public string Name { get; set; }

    [YamlMember(Alias = "font_size", ApplyNamingConventions = false)]
    public float? FontSize { get; set; }

    [YamlMember(Alias = "font_weight", ApplyNamingConventions = false)]
    public int? FontWeight { get; set; }

    [YamlMember(Alias = "font_italic", ApplyNamingConventions = false)]
    public bool? FontItalic { get; set; }

    [YamlMember(Alias = "font_family", ApplyNamingConventions = false)]
    public string FontFamily { get; set; }

    [YamlMember(Alias = "text_color", ApplyNamingConventions = false)]
    public string TextColor { get; set; }

    [YamlMember(Alias = "letter_spacing", ApplyNamingConventions = false)]
    public float? LetterSpacing { get; set; }
}
