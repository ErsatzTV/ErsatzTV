using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ErsatzTV.Core.Graphics;

public class SubtitlesGraphicsElement
{
    [YamlMember(Alias = "z_index", ApplyNamingConventions = false)]
    public int? ZIndex { get; set; }

    [YamlMember(Alias = "epg_entries", ApplyNamingConventions = false)]
    public int EpgEntries { get; set; }

    public string Template { get; set; }

    public static Option<SubtitlesGraphicsElement> FromYaml(string yaml)
    {
        try
        {
            // TODO: validate schema
            // if (await yamlScheduleValidator.ValidateSchedule(yaml, isImport) == false)
            // {
            //     return Option<YamlPlayoutDefinition>.None;
            // }

            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<SubtitlesGraphicsElement>(yaml);
        }
        catch (Exception)
        {
            return Option<SubtitlesGraphicsElement>.None;
        }
    }
}
