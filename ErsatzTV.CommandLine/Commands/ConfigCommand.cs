using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;

namespace ErsatzTV.CommandLine.Commands
{
    [Command("config", Description = "Configure ErsatzTV server url")]
    public class ConfigCommand : ICommand
    {
        [CommandParameter(0, Name = "server-url", Description = "The url of the ErsatzTV server")]
        public string ServerUrl { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            // TODO: validate URL

            string configFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ersatztv");

            string configFile = Path.Combine(configFolder, "cli.json");

            var config = new Config { ServerUrl = ServerUrl };
            string contents = JsonSerializer.Serialize(config);
            await File.WriteAllTextAsync(configFile, contents);
        }
    }
}
