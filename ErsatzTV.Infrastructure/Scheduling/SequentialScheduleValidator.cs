using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using YamlDotNet.RepresentationModel;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ErsatzTV.Infrastructure.Scheduling;

public class SequentialScheduleValidator(IFileSystem fileSystem, ILogger<SequentialScheduleValidator> logger)
    : ISequentialScheduleValidator
{
    public async Task<bool> ValidateSchedule(string yaml, bool isImport)
    {
        try
        {
            string schemaFileName = Path.Combine(
                FileSystemLayout.ResourcesCacheFolder,
                isImport ? "sequential-schedule-import.schema.json" : "sequential-schedule.schema.json");
            using StreamReader sr = fileSystem.File.OpenText(schemaFileName);
            await using var reader = new JsonTextReader(sr);
            var schema = JSchema.Load(reader);

            using var textReader = new StringReader(yaml);
            var yamlStream = new YamlStream();
            yamlStream.Load(textReader);
            var schedule = JObject.Parse(Convert(yamlStream));

            if (!schedule.IsValid(schema, out IList<string> errorMessages))
            {
                logger.LogWarning("Failed to validate sequential schedule definition: {ErrorMessages}", errorMessages);
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
        using var textReader = new StringReader(yaml);
        var yamlStream = new YamlStream();
        yamlStream.Load(textReader);
        var schedule = JObject.Parse(Convert(yamlStream));
        string formatted = JsonConvert.SerializeObject(schedule, Formatting.Indented);
        string[] lines = formatted.Split('\n');
        return string.Join('\n', lines.Select((line, index) => $"{index + 1,4}: {line}"));
    }

    public async Task<IList<string>> GetValidationMessages(string yaml, bool isImport)
    {
        try
        {
            string schemaFileName = Path.Combine(
                FileSystemLayout.ResourcesCacheFolder,
                isImport ? "sequential-schedule-import.schema.json" : "sequential-schedule.schema.json");
            using StreamReader sr = fileSystem.File.OpenText(schemaFileName);
            await using var reader = new JsonTextReader(sr);
            var schema = JSchema.Load(reader);

            using var textReader = new StringReader(yaml);
            var yamlStream = new YamlStream();
            yamlStream.Load(textReader);
            var schedule = JObject.Parse(Convert(yamlStream));

            return schedule.IsValid(schema, out IList<string> errorMessages) ? [] : errorMessages;
        }
        catch (Exception ex)
        {
            return [ex.Message];
        }
    }

    private static string Convert(YamlStream yamlStream)
    {
        var visitor = new YamlToJsonVisitor();
        yamlStream.Accept(visitor);
        return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(visitor.JsonString), Formatting.Indented);
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
