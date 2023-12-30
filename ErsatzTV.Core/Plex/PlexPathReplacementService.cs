using System.Runtime.InteropServices;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.FFmpeg.Runtime;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Plex;

public class PlexPathReplacementService : IPlexPathReplacementService
{
    private readonly ILogger<PlexPathReplacementService> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IRuntimeInfo _runtimeInfo;

    public PlexPathReplacementService(
        IMediaSourceRepository mediaSourceRepository,
        IRuntimeInfo runtimeInfo,
        ILogger<PlexPathReplacementService> logger)
    {
        _mediaSourceRepository = mediaSourceRepository;
        _runtimeInfo = runtimeInfo;
        _logger = logger;
    }

    public async Task<string> GetReplacementPlexPath(int libraryPathId, string path, bool log = true)
    {
        List<PlexPathReplacement> replacements =
            await _mediaSourceRepository.GetPlexPathReplacementsByLibraryId(libraryPathId);

        return GetReplacementPlexPath(replacements, path, log);
    }

    public string GetReplacementPlexPath(List<PlexPathReplacement> pathReplacements, string path, bool log = true)
    {
        Option<PlexPathReplacement> maybeReplacement = pathReplacements
            .SingleOrDefault(
                r =>
                {
                    if (string.IsNullOrWhiteSpace(r.PlexPath))
                    {
                        return false;
                    }

                    string separatorChar = IsWindows(r.PlexMediaSource) ? @"\" : @"/";
                    string prefix = r.PlexPath.EndsWith(separatorChar, StringComparison.OrdinalIgnoreCase)
                        ? r.PlexPath
                        : r.PlexPath + separatorChar;
                    return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
                });

        foreach (PlexPathReplacement replacement in maybeReplacement)
        {
            string finalPath = path.Replace(replacement.PlexPath, replacement.LocalPath);
            if (IsWindows(replacement.PlexMediaSource) && !_runtimeInfo.IsOSPlatform(OSPlatform.Windows))
            {
                finalPath = finalPath.Replace(@"\", @"/");
            }
            else if (!IsWindows(replacement.PlexMediaSource) && _runtimeInfo.IsOSPlatform(OSPlatform.Windows))
            {
                finalPath = finalPath.Replace(@"/", @"\");
            }

            if (log)
            {
                _logger.LogInformation(
                    "Replacing plex path {PlexPath} with {LocalPath} resulting in {FinalPath}",
                    replacement.PlexPath,
                    replacement.LocalPath,
                    finalPath);
            }

            return finalPath;
        }

        return path;
    }

    private static bool IsWindows(PlexMediaSource plexMediaSource) =>
        plexMediaSource.Platform.Equals("windows", StringComparison.OrdinalIgnoreCase);
}
