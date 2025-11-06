using System.Text.RegularExpressions;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Graphics;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Core.Metadata;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Scriban;
using Scriban.Runtime;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public partial class GraphicsElementLoader(
    TemplateFunctions templateFunctions,
    ILocalFileSystem localFileSystem,
    ITemplateDataRepository templateDataRepository,
    ILogger<GraphicsElementLoader> logger)
    : IGraphicsElementLoader
{
    public async Task<GraphicsEngineContext> LoadAll(
        GraphicsEngineContext context,
        List<PlayoutItemGraphicsElement> elements,
        CancellationToken cancellationToken)
    {
        // get max epg entries
        int epgEntries = await GetMaxEpgEntries(elements);

        // init template element variables once
        Dictionary<string, object> templateVariables =
            await InitTemplateVariables(context, epgEntries, cancellationToken);

        // subtitles are in separate files, so they need template variables for later processing
        context = context with { TemplateVariables = templateVariables };

        // fully process references (using template variables)
        foreach (PlayoutItemGraphicsElement reference in elements)
        {
            switch (reference.GraphicsElement.Kind)
            {
                case GraphicsElementKind.Text:
                {
                    Option<TextGraphicsElement> maybeElement = await LoadText(
                        reference.GraphicsElement.Path,
                        templateVariables);
                    if (maybeElement.IsNone)
                    {
                        logger.LogWarning(
                            "Failed to load text graphics element from file {Path}; ignoring",
                            reference.GraphicsElement.Path);
                    }

                    foreach (TextGraphicsElement element in maybeElement)
                    {
                        context.Elements.Add(new TextElementDataContext(element));
                    }

                    break;
                }
                case GraphicsElementKind.Image:
                {
                    Option<ImageGraphicsElement> maybeElement = await LoadImage(
                        reference.GraphicsElement.Path,
                        templateVariables);
                    if (maybeElement.IsNone)
                    {
                        logger.LogWarning(
                            "Failed to load image graphics element from file {Path}; ignoring",
                            reference.GraphicsElement.Path);
                    }

                    context.Elements.AddRange(maybeElement.Select(element => new ImageElementContext(element)));

                    break;
                }
                case GraphicsElementKind.Motion:
                {
                    Option<MotionGraphicsElement> maybeElement = await LoadMotion(
                        reference.GraphicsElement.Path,
                        templateVariables);
                    if (maybeElement.IsNone)
                    {
                        logger.LogWarning(
                            "Failed to load motion graphics element from file {Path}; ignoring",
                            reference.GraphicsElement.Path);
                    }

                    foreach (MotionGraphicsElement element in maybeElement)
                    {
                        context.Elements.Add(new MotionElementDataContext(element));
                    }

                    break;
                }
                case GraphicsElementKind.Subtitle:
                {
                    Option<SubtitleGraphicsElement> maybeElement = await LoadSubtitle(
                        reference.GraphicsElement.Path,
                        templateVariables);
                    if (maybeElement.IsNone)
                    {
                        logger.LogWarning(
                            "Failed to load subtitle graphics element from file {Path}; ignoring",
                            reference.GraphicsElement.Path);
                    }

                    foreach (SubtitleGraphicsElement element in maybeElement)
                    {
                        var variables = new Dictionary<string, string>();
                        if (!string.IsNullOrWhiteSpace(reference.Variables))
                        {
                            variables = JsonConvert.DeserializeObject<Dictionary<string, string>>(reference.Variables);
                        }

                        context.Elements.Add(new SubtitleElementDataContext(element, variables));
                    }

                    break;
                }
                default:
                    logger.LogInformation(
                        "Ignoring unsupported graphics element kind {Kind}",
                        nameof(reference.GraphicsElement.Kind));
                    break;
            }
        }

        return context;
    }

    private async Task<int> GetMaxEpgEntries(List<PlayoutItemGraphicsElement> elements)
    {
        var epgEntries = 0;

        IEnumerable<PlayoutItemGraphicsElement> elementsWithEpg = elements.Where(e =>
            e.GraphicsElement.Kind is GraphicsElementKind.Text or GraphicsElementKind.Subtitle);

        foreach (var reference in elementsWithEpg)
        {
            foreach (string line in await localFileSystem.ReadAllLines(reference.GraphicsElement.Path))
            {
                Match match = EpgEntriesRegex().Match(line);
                if (!match.Success || !int.TryParse(match.Groups[1].Value, out int value))
                {
                    continue;
                }

                epgEntries = Math.Max(epgEntries, value);
            }
        }

        return epgEntries;
    }

    private Task<Option<ImageGraphicsElement>> LoadImage(string fileName, Dictionary<string, object> variables) =>
        GetTemplatedYaml(fileName, variables).BindT(FromYaml<ImageGraphicsElement>);

    private Task<Option<TextGraphicsElement>> LoadText(string fileName, Dictionary<string, object> variables) =>
        GetTemplatedYaml(fileName, variables).BindT(FromYaml<TextGraphicsElement>);

    private Task<Option<MotionGraphicsElement>> LoadMotion(string fileName, Dictionary<string, object> variables) =>
        GetTemplatedYaml(fileName, variables).BindT(FromYaml<MotionGraphicsElement>);

    private Task<Option<SubtitleGraphicsElement>> LoadSubtitle(string fileName, Dictionary<string, object> variables) =>
        GetTemplatedYaml(fileName, variables).BindT(FromYaml<SubtitleGraphicsElement>);

    private async Task<Dictionary<string, object>> InitTemplateVariables(
        GraphicsEngineContext context,
        int epgEntries,
        CancellationToken cancellationToken)
    {
        // common variables
        var result = new Dictionary<string, object>
        {
            [FFmpegProfileTemplateDataKey.Resolution] = context.FrameSize,
            [FFmpegProfileTemplateDataKey.ScaledResolution] = context.SquarePixelFrameSize,
            [MediaItemTemplateDataKey.StreamSeek] = context.Seek,
            [MediaItemTemplateDataKey.Start] = context.ContentStartTime,
            [MediaItemTemplateDataKey.Stop] = context.ContentStartTime + context.Duration
        };

        // media item variables
        Option<Dictionary<string, object>> maybeTemplateData =
            await templateDataRepository.GetMediaItemTemplateData(context.MediaItem, cancellationToken);
        foreach (Dictionary<string, object> templateData in maybeTemplateData)
        {
            foreach (KeyValuePair<string, object> variable in templateData)
            {
                result.Add(variable.Key, variable.Value);
            }
        }

        // epg variables
        DateTimeOffset startTime = context.ContentStartTime + context.Seek;
        Option<Dictionary<string, object>> maybeEpgData =
            await templateDataRepository.GetEpgTemplateData(context.ChannelNumber, startTime, epgEntries);
        foreach (Dictionary<string, object> templateData in maybeEpgData)
        {
            foreach (KeyValuePair<string, object> variable in templateData)
            {
                result.Add(variable.Key, variable.Value);
            }
        }

        return result;
    }

    private async Task<Option<string>> GetTemplatedYaml(string fileName, Dictionary<string, object> variables)
    {
        string yaml = await localFileSystem.ReadAllText(fileName);
        try
        {
            var scriptObject = new ScriptObject();
            scriptObject.Import(variables, renamer: member => member.Name);
            scriptObject.Import("convert_timezone", templateFunctions.ConvertTimeZone);
            scriptObject.Import("format_datetime", templateFunctions.FormatDateTime);
            scriptObject.Import("get_directory_name", (string path) => Path.GetDirectoryName(path));
            scriptObject.Import("get_filename_without_extension", (string path) => Path.GetFileNameWithoutExtension(path));

            var context = new TemplateContext { MemberRenamer = member => member.Name };
            context.PushGlobal(scriptObject);
            return await Template.Parse(yaml).RenderAsync(context);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to render graphics element YAML definition as scriban template");
            return Option<string>.None;
        }
    }

    private Option<T> FromYaml<T>(string yaml)
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

            return deserializer.Deserialize<T>(yaml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load graphics element YAML definition");
            return Option<T>.None;
        }
    }

    [GeneratedRegex(@"epg_entries:\s*(\d+)")]
    private static partial Regex EpgEntriesRegex();
}
