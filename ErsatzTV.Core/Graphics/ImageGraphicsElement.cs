using ErsatzTV.FFmpeg.State;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ErsatzTV.Core.Graphics;

public class ImageGraphicsElement
{
    public string Image { get; set; }

    public int? Opacity { get; set; }

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

    public static async Task<Option<ImageGraphicsElement>> FromFile(string fileName)
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

            return deserializer.Deserialize<ImageGraphicsElement>(yaml);
        }
        catch (Exception)
        {
            return Option<ImageGraphicsElement>.None;
        }
    }
}