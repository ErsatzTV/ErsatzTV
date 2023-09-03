using System.Runtime.InteropServices;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Runtime;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Emby;

public class EmbyPathReplacementService : IEmbyPathReplacementService
{
    private readonly ILogger<EmbyPathReplacementService> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IRuntimeInfo _runtimeInfo;

    public EmbyPathReplacementService(
        IMediaSourceRepository mediaSourceRepository,
        IRuntimeInfo runtimeInfo,
        ILogger<EmbyPathReplacementService> logger)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _runtimeInfo = runtimeInfo;
        _logger = logger;
    }

    public async Task<string> GetReplacementEmbyPath(int libraryPathId, string path, bool log = true)
    {
        List<EmbyPathReplacement> replacements =
            await _mediaSourceRepository.GetEmbyPathReplacementsByLibraryId(libraryPathId);

        return GetReplacementEmbyPath(replacements, path, log);
    }

    public string GetReplacementEmbyPath(
        List<EmbyPathReplacement> pathReplacements,
        string path,
        bool log = true) =>
        GetReplacementEmbyPath(pathReplacements, path, _runtimeInfo.IsOSPlatform(OSPlatform.Windows), log);

    public string ReplaceNetworkPath(
        EmbyMediaSource embyMediaSource,
        string path,
        string networkPath,
        string replacement)
    {
        var replacements = new List<EmbyPathReplacement>
        {
            new() { EmbyPath = networkPath, LocalPath = replacement, EmbyMediaSource = embyMediaSource }
        };

        // we want to target the emby platform with the network path replacement
        bool isTargetPlatformWindows = embyMediaSource.OperatingSystem.ToLowerInvariant()
            .StartsWith("windows", StringComparison.OrdinalIgnoreCase);
        return GetReplacementEmbyPath(replacements, path, isTargetPlatformWindows, false);
    }

    private static bool IsWindows(EmbyMediaSource embyMediaSource, string path)
    {
        bool isUnc = Uri.TryCreate(path, UriKind.Absolute, out Uri uri) && uri.IsUnc;
        return isUnc || embyMediaSource.OperatingSystem.ToLowerInvariant()
            .StartsWith("windows", StringComparison.OrdinalIgnoreCase);
    }

    private string GetReplacementEmbyPath(
        List<EmbyPathReplacement> pathReplacements,
        string path,
        bool isTargetPlatformWindows,
        bool log)
    {
        Option<EmbyPathReplacement> maybeReplacement = pathReplacements
            .SingleOrDefault(
                r =>
                {
                    if (string.IsNullOrWhiteSpace(r.EmbyPath))
                    {
                        return false;
                    }

                    string separatorChar = IsWindows(r.EmbyMediaSource, path) ? @"\" : @"/";
                    string prefix = r.EmbyPath.EndsWith(separatorChar, StringComparison.OrdinalIgnoreCase)
                        ? r.EmbyPath
                        : r.EmbyPath + separatorChar;
                    return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
                });

        foreach (EmbyPathReplacement replacement in maybeReplacement)
        {
            string finalPath = path.Replace(replacement.EmbyPath, replacement.LocalPath);
            if (IsWindows(replacement.EmbyMediaSource, path) && !isTargetPlatformWindows)
            {
                finalPath = finalPath.Replace(@"\", @"/");
            }
            else if (!IsWindows(replacement.EmbyMediaSource, path) && isTargetPlatformWindows)
            {
                finalPath = finalPath.Replace(@"/", @"\");
            }

            if (log)
            {
                _logger.LogInformation(
                    "Replacing emby path {EmbyPath} with {LocalPath} resulting in {FinalPath}",
                    replacement.EmbyPath,
                    replacement.LocalPath,
                    finalPath);
            }

            return finalPath;
        }

        return path;
    }
}
