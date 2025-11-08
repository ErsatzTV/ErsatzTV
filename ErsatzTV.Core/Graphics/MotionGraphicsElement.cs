using ErsatzTV.FFmpeg.State;
using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Graphics;

public class MotionGraphicsElement : BaseGraphicsElement
{
    [YamlMember(Alias = "video_path", ApplyNamingConventions = false)]
    public string VideoPath { get; set; }

    // [YamlMember(Alias = "opacity_percent", ApplyNamingConventions = false)]
    // public int? OpacityPercent { get; set; }
    //
    // [YamlMember(Alias = "opacity_expression", ApplyNamingConventions = false)]
    // public string OpacityExpression { get; set; }

    [YamlMember(Alias = "start_seconds", ApplyNamingConventions = false)]
    public double? StartSeconds { get; set; }

    [YamlMember(Alias = "end_behavior", ApplyNamingConventions = false)]
    public MotionEndBehavior EndBehavior { get; set; } = MotionEndBehavior.Disappear;

    [YamlMember(Alias = "hold_seconds", ApplyNamingConventions = false)]
    public double? HoldSeconds { get; set; }

    public WatermarkLocation Location { get; set; }

    [YamlMember(Alias = "horizontal_margin_percent", ApplyNamingConventions = false)]
    public double? HorizontalMarginPercent { get; set; }

    [YamlMember(Alias = "vertical_margin_percent", ApplyNamingConventions = false)]
    public double? VerticalMarginPercent { get; set; }

    [YamlMember(Alias = "z_index", ApplyNamingConventions = false)]
    public int? ZIndex { get; set; }

    [YamlMember(Alias = "epg_entries", ApplyNamingConventions = false)]
    public int EpgEntries { get; set; }

    public bool Scale { get; set; }

    [YamlMember(Alias = "scale_width_percent", ApplyNamingConventions = false)]
    public double? ScaleWidthPercent { get; set; }
}
