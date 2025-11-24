using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using YamlDotNet.RepresentationModel;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonSchemaNet = Json.Schema;

namespace ErsatzTV.Infrastructure.Scheduling;

public class SequentialScheduleValidator(IFileSystem fileSystem, ILogger<SequentialScheduleValidator> logger)
    : ISequentialScheduleValidator
{
    public async Task<bool> ValidateSchedule(string yaml, bool isImport)
    {
        try
        {
            string schemaFileName = GetSchemaPath(isImport);
            string schemaText = await fileSystem.File.ReadAllTextAsync(schemaFileName);

            JsonSchemaNet.JsonSchema schema = JsonSchemaNet.JsonSchema.FromText(schemaText);

            string jsonString = ConvertYamlToJsonString(yaml);
            JsonNode jsonNode = JsonNode.Parse(jsonString);

            JsonSchemaNet.EvaluationResults result = schema.Evaluate(jsonNode);

            if (!result.IsValid)
            {
                logger.LogWarning("Sequential schedule definition failed validation");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected error while validating sequential schedule definition");
        }

        return false;
    }

    public string ToJson(string yaml)
    {
        string jsonString = ConvertYamlToJsonString(yaml);
        var schedule = JObject.Parse(jsonString);

        string formatted = JsonConvert.SerializeObject(schedule, Formatting.Indented);
        string[] lines = formatted.Split('\n');
        return string.Join('\n', lines.Select((line, index) => $"{index + 1,4}: {line}"));
    }

    // limited to 1000/hr, but only called manually from UI
    public async Task<IList<string>> GetValidationMessages(string yaml, bool isImport)
    {
        try
        {
            string schemaFileName = GetSchemaPath(isImport);

            using StreamReader sr = fileSystem.File.OpenText(schemaFileName);
            await using var reader = new JsonTextReader(sr);
            var schema = JSchema.Load(reader);

            string jsonString = ConvertYamlToJsonString(yaml);
            var schedule = JObject.Parse(jsonString);

            return schedule.IsValid(schema, out IList<string> errorMessages) ? [] : errorMessages;
        }
        catch (Exception ex)
        {
            return [ex.Message];
        }
    }

    private static string ConvertYamlToJsonString(string yaml)
    {
        using var textReader = new StringReader(yaml);
        var yamlStream = new YamlStream();
        yamlStream.Load(textReader);

        var visitor = new YamlToJsonVisitor();
        yamlStream.Accept(visitor);
        return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(visitor.JsonString), Formatting.Indented);
    }

    private static string GetSchemaPath(bool isImport)
    {
        return Path.Combine(
            FileSystemLayout.ResourcesCacheFolder,
            isImport ? "sequential-schedule-import.schema.json" : "sequential-schedule.schema.json");
    }

    private sealed class YamlToJsonVisitor : IYamlVisitor
    {
        private readonly JsonSerializerOptions _options = new() { WriteIndented = false };
        private object _currentValue;

        public string JsonString => JsonSerializer.Serialize(_currentValue, _options);

        public void Visit(YamlScalarNode scalar)
        {
            string value = scalar.Value;

            if (string.IsNullOrEmpty(value))
            {
                _currentValue = null;
                return;
            }

            // Try to parse in order of most specific to most general
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                _currentValue = true;
                return;
            }

            if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                _currentValue = false;
                return;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intResult))
            {
                _currentValue = intResult;
                return;
            }

            if (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out double doubleResult))
            {
                _currentValue = doubleResult;
                return;
            }

            _currentValue = value;
        }

        public void Visit(YamlSequenceNode sequence)
        {
            var array = new List<object>();
            foreach (YamlNode node in sequence.Children)
            {
                node.Accept(this);
                array.Add(_currentValue);
            }

            _currentValue = array;
        }

        public void Visit(YamlMappingNode mapping)
        {
            Dictionary<string, object> dict = new(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
            {
                string key = entry.Key switch
                {
                    YamlScalarNode scalar => scalar.Value,
                    _ => entry.Key.ToString()
                };

                entry.Value.Accept(this);
                dict[key!] = _currentValue;
            }

            _currentValue = dict;
        }

        public void Visit(YamlDocument document) => document.RootNode.Accept(this);
        public void Visit(YamlStream stream) => stream.Documents[0].RootNode.Accept(this);
    }
}
