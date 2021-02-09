﻿using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegLocator : IFFmpegLocator
    {
        private readonly IConfigElementRepository _configElementRepository;

        public FFmpegLocator(IConfigElementRepository configElementRepository) =>
            _configElementRepository = configElementRepository;

        public async Task<Option<string>> ValidatePath(string executableBase, ConfigElementKey key)
        {
            Option<ConfigElement> setting = await _configElementRepository.Get(key);

            return await setting.MatchAsync(
                async ce =>
                {
                    if (File.Exists(ce.Value))
                    {
                        return ce.Value;
                    }

                    // configured path was incorrect
                    await _configElementRepository.Delete(ce);

                    return await LocateExecutableAsync(executableBase, key);
                },
                async () => await LocateExecutableAsync(executableBase, key));
        }

        private async Task<Option<string>> LocateExecutableAsync(string executableBase, ConfigElementKey key)
        {
            Option<string> maybePath = await LocateExecutableOnPathAsync(executableBase);
            return await maybePath.MatchAsync(
                async path =>
                {
                    await _configElementRepository.Add(new ConfigElement { Key = key.Key, Value = path });
                    return Some(path);
                },
                () => None);
        }

        private async Task<Option<string>> LocateExecutableOnPathAsync(string executableBase)
        {
            string locateCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            string executable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"{executableBase}.exe"
                : executableBase;
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    FileName = locateCommand,
                    Arguments = executable,
                    RedirectStandardOutput = true
                }
            };
            p.Start();
            string path = (await p.StandardOutput.ReadToEndAsync()).Trim();
            await p.WaitForExitAsync();
            return p.ExitCode == 0 ? Some(path) : None;
        }
    }
}
