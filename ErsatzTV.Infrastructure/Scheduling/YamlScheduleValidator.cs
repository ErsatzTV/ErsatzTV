using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using YamlDotNet.Serialization;

namespace ErsatzTV.Infrastructure.Scheduling;

public class YamlScheduleValidator(ILogger<YamlScheduleValidator> logger) : IYamlScheduleValidator
{
    public async Task<bool> ValidateSchedule(string yaml)
    {
        try
        {
            string schemaFileName = Path.Combine(FileSystemLayout.ResourcesCacheFolder, "yaml-playout.schema.json");
            using StreamReader sr = File.OpenText(schemaFileName);
            await using var reader = new JsonTextReader(sr);
            var schema = JSchema.Load(reader);

            string jsonString = ToJson(yaml);
            var schedule = JObject.Parse(jsonString);

            if (!schedule.IsValid(schema, out IList<string> errorMessages))
            {
                logger.LogWarning("Failed to validate YAML schedule definition: {ErrorMessages}", errorMessages);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected error while validating YAML schedule definition");
        }

        return false;
    }

    private static string ToJson(string yaml)
    {
        using var reader = new StringReader(yaml);
        var deserializer = new Deserializer();
        object yamlObject = deserializer.Deserialize(reader);
        ISerializer serializer = new SerializerBuilder()
            .JsonCompatible()
            .Build();
        return serializer.Serialize(yamlObject).Trim();
    }
}
