using System.Globalization;
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

public class YamlScheduleValidator(ILogger<YamlScheduleValidator> logger) : IYamlScheduleValidator
{
    public async Task<bool> ValidateSchedule(string yaml, bool isImport)
    {
        try
        {
            string schemaFileName = Path.Combine(FileSystemLayout.ResourcesCacheFolder,
                isImport ? "yaml-playout-import.schema.json" : "yaml-playout.schema.json");
            using StreamReader sr = File.OpenText(schemaFileName);
            await using var reader = new JsonTextReader(sr);
            var schema = JSchema.Load(reader);

            using var textReader = new StringReader(yaml);
            var yamlStream = new YamlStream();
            yamlStream.Load(textReader);
            var schedule = JObject.Parse(Convert(yamlStream));

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

    private static string Convert(YamlStream yamlStream)
    {
        var visitor = new YamlToJsonVisitor();
        yamlStream.Accept(visitor);
        return visitor.JsonString;
    }

    private sealed class YamlToJsonVisitor : IYamlVisitor
    {
        private readonly JsonSerializerOptions _options = new() { WriteIndented = false, };
        private object _currentValue;

        public string JsonString => JsonSerializer.Serialize(_currentValue, _options);

        public void Visit(YamlScalarNode scalar)
        {
            var value = scalar.Value;

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

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intResult))
            {
                _currentValue = intResult;
                return;
            }

            if (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var doubleResult))
            {
                _currentValue = doubleResult;
                return;
            }

            _currentValue = value;
        }

        public void Visit(YamlSequenceNode sequence)
        {
            var array = new List<object>();
            foreach (var node in sequence.Children)
            {
                node.Accept(this);
                array.Add(_currentValue);
            }

            _currentValue = array;
        }

        public void Visit(YamlMappingNode mapping)
        {
            Dictionary<string, object> dict = new(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in mapping.Children)
            {
                var key = entry.Key switch
                {
                    YamlScalarNode scalar => scalar.Value,
                    _ => entry.Key.ToString(),
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
