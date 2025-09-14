using ErsatzTV.FFmpeg.State;
using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Graphics;

public class ImageGraphicsElement
{
    public string Image { get; set; }

    [YamlMember(Alias = "opacity_percent", ApplyNamingConventions = false)]
    public int? OpacityPercent { get; set; }

    [YamlMember(Alias = "opacity_expression", ApplyNamingConventions = false)]
    public string OpacityExpression { get; set; }

    public WatermarkLocation Location { get; set; }

    [YamlMember(Alias = "horizontal_margin_percent", ApplyNamingConventions = false)]
    public double? HorizontalMarginPercent { get; set; }

    [YamlMember(Alias = "vertical_margin_percent", ApplyNamingConventions = false)]
    public double? VerticalMarginPercent { get; set; }

    [YamlMember(Alias = "location_x", ApplyNamingConventions = false)]
    public double? LocationX { get; set; }

    [YamlMember(Alias = "location_y", ApplyNamingConventions = false)]
    public double? LocationY { get; set; }

    [YamlMember(Alias = "z_index", ApplyNamingConventions = false)]
    public int? ZIndex { get; set; }

    public bool Scale { get; set; }

    [YamlMember(Alias = "scale_width_percent", ApplyNamingConventions = false)]
    public double? ScaleWidthPercent { get; set; }
}
