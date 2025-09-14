using ErsatzTV.FFmpeg.State;
using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Graphics;

public class TextGraphicsElement
{
    [YamlMember(Alias = "opacity_percent", ApplyNamingConventions = false)]
    public int? OpacityPercent { get; set; }

    [YamlMember(Alias = "opacity_expression", ApplyNamingConventions = false)]
    public string OpacityExpression { get; set; }

    public WatermarkLocation Location { get; set; }

    [YamlMember(Alias = "horizontal_margin_percent", ApplyNamingConventions = false)]
    public double? HorizontalMarginPercent { get; set; }

    [YamlMember(Alias = "vertical_margin_percent", ApplyNamingConventions = false)]
    public double? VerticalMarginPercent { get; set; }

    [YamlMember(Alias = "horizontal_alignment", ApplyNamingConventions = false)]
    public string HorizontalAlignment { get; set; }

    [YamlMember(Alias = "location_x", ApplyNamingConventions = false)]
    public double? LocationX { get; set; }

    [YamlMember(Alias = "location_y", ApplyNamingConventions = false)]
    public double? LocationY { get; set; }

    [YamlMember(Alias = "z_index", ApplyNamingConventions = false)]
    public int? ZIndex { get; set; }

    public List<StyleDefinition> Styles { get; set; } = [];

    [YamlMember(Alias = "base_style", ApplyNamingConventions = false)]
    public string BaseStyle { get; set; }

    [YamlMember(Alias = "include_fonts_from", ApplyNamingConventions = false)]
    public string IncludeFontsFrom { get; set; }

    [YamlMember(Alias = "epg_entries", ApplyNamingConventions = false)]
    public int EpgEntries { get; set; }

    public string Text { get; set; }
}

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
