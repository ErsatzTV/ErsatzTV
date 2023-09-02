﻿using System.Runtime.InteropServices;

namespace ErsatzTV.FFmpeg.Filter;

public class SubtitlesFilter : BaseFilter
{
    private readonly string _fontsDir;
    private readonly SubtitleInputFile _subtitleInputFile;

    public SubtitlesFilter(string fontsDir, SubtitleInputFile subtitleInputFile)
    {
        _fontsDir = fontsDir;
        _subtitleInputFile = subtitleInputFile;
    }

    public override string Filter
    {
        get
        {
            string fontsDir = _fontsDir;
            string effectiveFile = _subtitleInputFile.Path;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fontsDir = fontsDir
                    .Replace(@"\", @"/\")
                    .Replace(@":/", @"\\:/");

                effectiveFile = effectiveFile
                    .Replace(@"\", @"/\");

                if (!effectiveFile.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    effectiveFile = effectiveFile.Replace(@":/", @"\\:/");
                }
            }

            // escape brackets after escaping for windows
            effectiveFile = effectiveFile
                .Replace(@"[", @"\[")
                .Replace(@"]", @"\]")
                .Replace(@"http://localhost:", @"http\\://localhost\\:");

            return $"subtitles={effectiveFile}:fontsdir={fontsDir}";
        }
    }

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Software };
}
