using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Runtime;
using LanguageExt;
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

    public async Task<string> GetReplacementEmbyPath(int libraryPathId, string path)
    {
        List<EmbyPathReplacement> replacements =
            await _mediaSourceRepository.GetEmbyPathReplacementsByLibraryId(libraryPathId);

        return GetReplacementEmbyPath(replacements, path);
    }

    public string GetReplacementEmbyPath(
        List<EmbyPathReplacement> pathReplacements,
        string path,
        bool log = true)
    {
        Option<EmbyPathReplacement> maybeReplacement = pathReplacements
            .SingleOrDefault(
                r =>
                {
                    string separatorChar = IsWindows(r.EmbyMediaSource, path) ? @"\" : @"/";
                    string prefix = r.EmbyPath.EndsWith(separatorChar)
                        ? r.EmbyPath
                        : r.EmbyPath + separatorChar;
                    return path.StartsWith(prefix);
                });

        return maybeReplacement.Match(
            replacement =>
            {
                string finalPath = path.Replace(replacement.EmbyPath, replacement.LocalPath);
                if (IsWindows(replacement.EmbyMediaSource, path) && !_runtimeInfo.IsOSPlatform(OSPlatform.Windows))
                {
                    finalPath = finalPath.Replace(@"\", @"/");
                }
                else if (!IsWindows(replacement.EmbyMediaSource, path) &&
                         _runtimeInfo.IsOSPlatform(OSPlatform.Windows))
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
            },
            () => path);
    }

    private static bool IsWindows(EmbyMediaSource embyMediaSource, string path)
    {
        bool isUnc = Uri.TryCreate(path, UriKind.Absolute, out Uri uri) && uri.IsUnc;
        return isUnc || embyMediaSource.OperatingSystem.ToLowerInvariant().StartsWith("windows");
    }
}