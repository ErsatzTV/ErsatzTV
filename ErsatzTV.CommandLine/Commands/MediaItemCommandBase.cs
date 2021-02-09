using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using LanguageExt;
using LanguageExt.Common;

namespace ErsatzTV.CommandLine.Commands
{
    public abstract class MediaItemCommandBase : ICommand
    {
        [CommandOption("folder", 'f', Description = "Folder to search for media items")]
        public string Folder { get; set; }

        [CommandOption("pattern", 'p', Description = "File search pattern")]
        public string SearchPattern { get; set; }

        public abstract ValueTask ExecuteAsync(IConsole console);

        protected async Task<Either<Error, List<string>>> GetFileNames()
        {
            if (Console.IsInputRedirected)
            {
                await using Stream standardInput = Console.OpenStandardInput();
                using var streamReader = new StreamReader(standardInput);
                string input = await streamReader.ReadToEndAsync();
                return input.Trim().Split("\n").Map(s => s.Trim()).ToList();
            }

            if (string.IsNullOrWhiteSpace(Folder) || string.IsNullOrWhiteSpace(SearchPattern))
            {
                return Error.New(
                    "--folder and --pattern are required when file names are not passed on standard input");
            }

            return Directory.GetFiles(Folder, SearchPattern, SearchOption.AllDirectories).ToList();
        }
    }
}
