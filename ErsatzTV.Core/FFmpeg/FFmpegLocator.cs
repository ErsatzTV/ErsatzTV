using System.Diagnostics;
using System.Runtime.InteropServices;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegLocator(IConfigElementRepository configElementRepository) : IFFmpegLocator
{
    public async Task<Option<string>> ValidatePath(
        string executableBase,
        ConfigElementKey key,
        CancellationToken cancellationToken)
    {
        Option<ConfigElement> setting = await configElementRepository.GetConfigElement(key, cancellationToken);

        return await setting.MatchAsync(
            async ce =>
            {
                if (File.Exists(ce.Value))
                {
                    return ce.Value;
                }

                // configured path was incorrect
                await configElementRepository.Delete(ce, cancellationToken);

                return await LocateExecutableAsync(executableBase, key, cancellationToken);
            },
            async () => await LocateExecutableAsync(executableBase, key, cancellationToken));
    }

    private async Task<Option<string>> LocateExecutableAsync(
        string executableBase,
        ConfigElementKey key,
        CancellationToken cancellationToken)
    {
        Option<string> maybePath = await LocateExecutableOnPathAsync(executableBase);
        return await maybePath.MatchAsync(
            async path =>
            {
                await configElementRepository.Upsert(key, path, cancellationToken);
                return Some(path);
            },
            () => None);
    }

    private static async Task<Option<string>> LocateExecutableOnPathAsync(string executableBase)
    {
        string executable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"{executableBase}.exe"
            : executableBase;

        string processFileName = Environment.ProcessPath ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(processFileName))
        {
            string localFileName = Path.Combine(Path.GetDirectoryName(processFileName) ?? string.Empty, executable);
            if (File.Exists(localFileName))
            {
                return localFileName;
            }
        }

        string locateCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
        using var p = new Process();
        p.StartInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            FileName = locateCommand,
            Arguments = executable,
            RedirectStandardOutput = true
        };
        p.Start();
        string path = (await p.StandardOutput.ReadToEndAsync()).Trim();
        await p.WaitForExitAsync();
        return p.ExitCode == 0 ? Some(path) : None;
    }
}
