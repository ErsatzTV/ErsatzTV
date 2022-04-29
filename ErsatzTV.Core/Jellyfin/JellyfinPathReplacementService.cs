﻿using System.Runtime.InteropServices;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Runtime;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Jellyfin;

public class JellyfinPathReplacementService : IJellyfinPathReplacementService
{
    private readonly ILogger<JellyfinPathReplacementService> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IRuntimeInfo _runtimeInfo;

    public JellyfinPathReplacementService(
        IMediaSourceRepository mediaSourceRepository,
        IRuntimeInfo runtimeInfo,
        ILogger<JellyfinPathReplacementService> logger)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _runtimeInfo = runtimeInfo;
        _logger = logger;
    }

    public async Task<string> GetReplacementJellyfinPath(int libraryPathId, string path, bool log = true)
    {
        List<JellyfinPathReplacement> replacements =
            await _mediaSourceRepository.GetJellyfinPathReplacementsByLibraryId(libraryPathId);

        return GetReplacementJellyfinPath(replacements, path, log);
    }

    public string GetReplacementJellyfinPath(
        List<JellyfinPathReplacement> pathReplacements,
        string path,
        bool log = true) =>
        GetReplacementJellyfinPath(pathReplacements, path, _runtimeInfo.IsOSPlatform(OSPlatform.Windows), log);

    public string ReplaceNetworkPath(
        JellyfinMediaSource jellyfinMediaSource,
        string path,
        string networkPath,
        string replacement)
    {
        var replacements = new List<JellyfinPathReplacement>
        {
            new() { JellyfinPath = networkPath, LocalPath = replacement, JellyfinMediaSource = jellyfinMediaSource }
        };

        // we want to target the jellyfin platform with the network path replacement
        bool isTargetPlatformWindows = jellyfinMediaSource.OperatingSystem.ToLowerInvariant().StartsWith("windows");
        return GetReplacementJellyfinPath(replacements, path, isTargetPlatformWindows, false);
    }

    private static bool IsWindows(JellyfinMediaSource jellyfinMediaSource, string path)
    {
        bool isUnc = Uri.TryCreate(path, UriKind.Absolute, out Uri uri) && uri.IsUnc;
        return isUnc || jellyfinMediaSource.OperatingSystem.ToLowerInvariant().StartsWith("windows");
    }
    
    private string GetReplacementJellyfinPath(
        List<JellyfinPathReplacement> pathReplacements,
        string path,
        bool isTargetPlatformWindows,
        bool log)
    {
        Option<JellyfinPathReplacement> maybeReplacement = pathReplacements
            .SingleOrDefault(
                r =>
                {
                    if (string.IsNullOrWhiteSpace(r.JellyfinPath))
                    {
                        return false;
                    }

                    string separatorChar = IsWindows(r.JellyfinMediaSource, path) ? @"\" : @"/";
                    string prefix = r.JellyfinPath.EndsWith(separatorChar)
                        ? r.JellyfinPath
                        : r.JellyfinPath + separatorChar;
                    return path.StartsWith(prefix);
                });

        foreach (JellyfinPathReplacement replacement in maybeReplacement)
        {
            string finalPath = path.Replace(replacement.JellyfinPath, replacement.LocalPath);
            if (IsWindows(replacement.JellyfinMediaSource, path) && !isTargetPlatformWindows)
            {
                finalPath = finalPath.Replace(@"\", @"/");
            }
            else if (!IsWindows(replacement.JellyfinMediaSource, path) && isTargetPlatformWindows)
            {
                finalPath = finalPath.Replace(@"/", @"\");
            }

            if (log)
            {
                _logger.LogInformation(
                    "Replacing jellyfin path {JellyfinPath} with {LocalPath} resulting in {FinalPath}",
                    replacement.JellyfinPath,
                    replacement.LocalPath,
                    finalPath);
            }

            return finalPath;
        }

        return path;
    }
}
