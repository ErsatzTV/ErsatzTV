using ErsatzTV.FFmpeg.State;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ErsatzTV.Core.Graphics;

public class TextGraphicsElement
{
    public int? Opacity { get; set; }

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

    [YamlMember(Alias = "font_family", ApplyNamingConventions = false)]
    public string FontFamily { get; set; }
    [YamlMember(Alias = "font_size", ApplyNamingConventions = false)]
    public int? FontSize { get; set; }
    [YamlMember(Alias = "font_color", ApplyNamingConventions = false)]
    public string FontColor { get; set; }

    [YamlMember(Alias = "epg_entries", ApplyNamingConventions = false)]
    public int EpgEntries { get; set; }

    public string Text { get; set; }

    public static async Task<Option<TextGraphicsElement>> FromFile(string fileName)
    {
        try
        {
            string yaml = await File.ReadAllTextAsync(fileName);

            // TODO: validate schema
            // if (await yamlScheduleValidator.ValidateSchedule(yaml, isImport) == false)
            // {
            //     return Option<YamlPlayoutDefinition>.None;
            // }

            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<TextGraphicsElement>(yaml);
        }
        catch (Exception)
        {
            return Option<TextGraphicsElement>.None;
        }
    }
}