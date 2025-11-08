using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ErsatzTV.Infrastructure.FFmpeg;

public class MpegTsScriptService(
    ILocalFileSystem localFileSystem,
    ITempFilePool tempFilePool,
    ILogger<MpegTsScriptService> logger) : IMpegTsScriptService
{
    private static readonly ConcurrentDictionary<string, MpegTsScript> Scripts = new();

    public async Task RefreshScripts()
    {
        foreach (string folder in localFileSystem.ListSubdirectories(FileSystemLayout.MpegTsScriptsFolder))
        {
            string definition = Path.Combine(folder, "mpegts.yml");
            if (!Scripts.ContainsKey(folder) && localFileSystem.FileExists(definition))
            {
                Option<MpegTsScript> maybeScript = FromYaml(await localFileSystem.ReadAllText(definition));
                foreach (var script in maybeScript)
                {
                    script.Id = Path.GetFileName(folder);
                    Scripts[folder] = script;
                }
            }
        }
    }

    public List<MpegTsScript> GetScripts() => Scripts.Values.ToList();

    public async Task<Option<Command>> Execute(MpegTsScript script, Channel channel, string hlsUrl, string ffmpegPath)
    {
        string scriptFolder = string.Empty;
        foreach (KeyValuePair<string, MpegTsScript> kvp in Scripts.Where(kvp => kvp.Value == script))
        {
            scriptFolder = kvp.Key;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string scriptInput = Path.Combine(scriptFolder, script.WindowsScript);
            if (File.Exists(scriptInput))
            {
                Option<string> maybeScript = await GetTemplatedScript(
                    scriptInput,
                    hlsUrl,
                    channel.Name,
                    ffmpegPath);
                foreach (string finalScript in maybeScript)
                {
                    var fileName = $"{tempFilePool.GetNextTempFile(TempFileCategory.MpegTsScript)}.bat";
                    await File.WriteAllTextAsync(fileName, finalScript);
                    return Cli.Wrap(fileName);
                }
            }
        }
        else
        {
            string scriptInput = Path.Combine(scriptFolder, script.LinuxScript);
            if (File.Exists(scriptInput))
            {
                Option<string> maybeScript = await GetTemplatedScript(
                    scriptInput,
                    hlsUrl,
                    channel.Name,
                    ffmpegPath);
                foreach (string finalScript in maybeScript)
                {
                    string fileName = tempFilePool.GetNextTempFile(TempFileCategory.MpegTsScript);
                    await File.WriteAllTextAsync(fileName, finalScript);
                    return Cli.Wrap("bash").WithArguments([fileName]);
                }
            }
        }

        return [];
    }

    private async Task<Option<string>> GetTemplatedScript(
        string fileName,
        string hlsUrl,
        string channelName,
        string ffmpegPath)
    {
        string script = await localFileSystem.ReadAllText(fileName);
        try
        {
            var data = new Dictionary<string, string>
            {
                ["HlsUrl"] = hlsUrl,
                ["ChannelName"] = channelName,
                ["FFmpegPath"] = ffmpegPath
            };

            var scriptObject = new ScriptObject();
            scriptObject.Import(data, renamer: member => member.Name);
            var context = new TemplateContext { MemberRenamer = member => member.Name };
            context.PushGlobal(scriptObject);
            return await Template.Parse(script).RenderAsync(context);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to render mpegts script as scriban template");
            return Option<string>.None;
        }
    }

    private Option<MpegTsScript> FromYaml(string yaml)
    {
        try
        {
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<MpegTsScript>(yaml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load mpegts script YAML definition");
            return Option<MpegTsScript>.None;
        }
    }
}
